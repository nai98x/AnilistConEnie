using System.Net;
using System.Net.Http.Headers;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Exceptions;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace AnilistConEnie.Infrastructure.Anilist;

/// <summary>
/// Núcleo del acceso a AniList. Posee un único <see cref="GraphQLHttpClient"/> reutilizado y
/// centraliza la resiliencia: reintentos con backoff (Polly), espera del <c>Retry-After</c> ante
/// 429, traducción de errores de transporte a excepciones de dominio y lectura del rate limit.
/// Es singleton; los métodos de consulta concretos viven en <see cref="AnilistClient"/>.
/// </summary>
internal sealed class AnilistGraphQLExecutor : IDisposable
{
    private const string Endpoint = "https://graphql.anilist.co";

    private readonly GraphQLHttpClient _client;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<AnilistGraphQLExecutor> _logger;

    // Espera proactiva: cuando una respuesta informa que no quedan requests en la ventana
    // (Remaining <= 0), guardamos hasta cuándo hay que frenar para no provocar un 429.
    private readonly object _rateLimitLock = new();
    private DateTimeOffset? _pausedUntil;

    public AnilistGraphQLExecutor(ILogger<AnilistGraphQLExecutor> logger)
    {
        _logger = logger;
        _client = new GraphQLHttpClient(
            new GraphQLHttpClientOptions { EndPoint = new Uri(Endpoint) },
            new NewtonsoftJsonSerializer(),
            new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
        _pipeline = BuildPipeline();
    }
    
    /// <summary>
    /// Igual que <see cref="SendQueryAsync{T}"/> pero adjuntando el token OAuth del usuario en el
    /// header <c>Authorization</c> (consultas sobre <c>Viewer</c> y demás datos privados).
    /// </summary>
    public Task<AnilistResponse<T>> SendAuthenticatedQueryAsync<T>(GraphQLRequest request, string accessToken, CancellationToken cancellationToken) =>
        SendQueryAsync<T>(new AuthenticatedGraphQLHttpRequest(request, accessToken), cancellationToken);

    /// <summary>
    /// Ejecuta una query y devuelve los datos tipados junto al estado de rate limit. Aplica la
    /// pipeline de reintentos y traduce los fallos a <see cref="AnilistApiException"/> y derivados.
    /// </summary>
    public async Task<AnilistResponse<T>> SendQueryAsync<T>(GraphQLRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await _pipeline.ExecuteAsync(async token =>
            {
                // Antes de cada intento (incluidos los reintentos), respetar la ventana si está agotada.
                await WaitForRateLimitWindowAsync(token);

                GraphQLResponse<T> response = await _client.SendQueryAsync<T>(request, token);

                // Errores a nivel GraphQL (HTTP 200 con un array "errors"): query inválida, media
                // inexistente, etc. No son transitorios, así que no se reintentan.
                if (response.Errors is { Length: > 0 })
                {
                    string detail = string.Join("; ", response.Errors.Select(e => e.Message));
                    throw new AnilistApiException($"AniList devolvió errores: {detail}");
                }

                GraphQLHttpResponse<T> http = response.AsGraphQLHttpResponse();
                AnilistRateLimit rateLimit = ReadRateLimit(http.ResponseHeaders);
                UpdateRateLimitState(rateLimit);
                return new AnilistResponse<T>(response.Data, rateLimit);
            }, cancellationToken);
        }
        catch (GraphQLHttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new AnilistRateLimitException(GetRetryAfter(ex.ResponseHeaders), ex);
        }
        catch (GraphQLHttpRequestException ex) when ((int)ex.StatusCode >= 500)
        {
            throw new AnilistServerErrorException((int)ex.StatusCode, ex);
        }
        catch (GraphQLHttpRequestException ex)
        {
            throw new AnilistApiException($"AniList respondió HTTP {(int)ex.StatusCode} ({ex.StatusCode}).", ex);
        }
    }

    private ResiliencePipeline BuildPipeline() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(500),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<GraphQLHttpRequestException>(IsTransient),
                // Ante 429 respetamos el Retry-After de AniList; en el resto usamos el backoff
                // exponencial por defecto (devolviendo null).
                DelayGenerator = static args =>
                {
                    if (args.Outcome.Exception is GraphQLHttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } ex)
                    {
                        return ValueTask.FromResult(GetRetryAfter(ex.ResponseHeaders));
                    }

                    return ValueTask.FromResult<TimeSpan?>(null);
                },
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Reintentando request a AniList (intento {Attempt}) tras esperar {Delay}",
                        args.AttemptNumber + 1,
                        args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

    /// <summary>
    /// Si una respuesta previa dejó la ventana de rate limit agotada, espera hasta el reset antes
    /// de continuar. Varias consultas concurrentes esperan hasta el mismo instante y luego siguen.
    /// </summary>
    private async Task WaitForRateLimitWindowAsync(CancellationToken token)
    {
        DateTimeOffset? until;
        lock (_rateLimitLock) until = _pausedUntil;

        if (until is not { } resetAt) return;

        TimeSpan wait = resetAt - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero)
        {
            _logger.LogWarning("Rate limit de AniList agotado; esperando {Wait} hasta el reset de la ventana.", wait);
            await Task.Delay(wait, token);
        }

        // Limpiar la pausa solo si nadie la movió a un futuro más lejano mientras esperábamos.
        lock (_rateLimitLock)
        {
            if (_pausedUntil == until) _pausedUntil = null;
        }
    }

    /// <summary>
    /// Registra la pausa proactiva cuando la respuesta indica que no quedan requests en la ventana.
    /// Se agrega 1s de margen para tolerar desfasajes de reloj con el reset informado por AniList.
    /// </summary>
    private void UpdateRateLimitState(AnilistRateLimit rateLimit)
    {
        if (rateLimit is { Remaining: <= 0, ResetAt: { } reset })
        {
            lock (_rateLimitLock) _pausedUntil = reset.AddSeconds(1);
        }
    }

    /// <summary>Errores HTTP que vale la pena reintentar: rate limit (429) y errores de servidor (5xx).</summary>
    private static bool IsTransient(GraphQLHttpRequestException ex) =>
        ex.StatusCode == HttpStatusCode.TooManyRequests || (int)ex.StatusCode >= 500;

    private static TimeSpan? GetRetryAfter(HttpResponseHeaders? headers)
    {
        RetryConditionHeaderValue? retryAfter = headers?.RetryAfter;
        if (retryAfter is null) return null;

        if (retryAfter.Delta is { } delta) return delta;
        if (retryAfter.Date is { } date)
        {
            TimeSpan wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }

    private static AnilistRateLimit ReadRateLimit(HttpResponseHeaders headers)
    {
        return new AnilistRateLimit
        {
            Limit = TryGetInt(headers, "X-RateLimit-Limit", out int limit) ? limit : null,
            Remaining = TryGetInt(headers, "X-RateLimit-Remaining", out int remaining) ? remaining : null,
            // AniList informa el reset como epoch en segundos (Unix time).
            ResetAt = TryGetLong(headers, "X-RateLimit-Reset", out long reset) ? DateTimeOffset.FromUnixTimeSeconds(reset) : null
        };
    }

    private static bool TryGetInt(HttpResponseHeaders headers, string name, out int value)
    {
        value = 0;
        return headers.TryGetValues(name, out IEnumerable<string>? values)
               && int.TryParse(values.FirstOrDefault(), out value);
    }

    private static bool TryGetLong(HttpResponseHeaders headers, string name, out long value)
    {
        value = 0;
        return headers.TryGetValues(name, out IEnumerable<string>? values)
               && long.TryParse(values.FirstOrDefault(), out value);
    }

    public void Dispose() => _client.Dispose();
}

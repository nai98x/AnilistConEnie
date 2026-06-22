namespace AnilistConEnie.Model.Exceptions;

/// <summary>
/// Excepción base para cualquier error al consultar la API de AniList. La capa que consume
/// <c>IAnilistClient</c> puede capturar este tipo (o sus derivados) sin conocer detalles del
/// transporte GraphQL/HTTP.
/// </summary>
public class AnilistApiException : Exception
{
    public AnilistApiException(string message) : base(message) { }

    public AnilistApiException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Se lanza cuando AniList responde 429 (Too Many Requests) y los reintentos se agotaron.
/// <see cref="RetryAfter"/> indica cuánto esperar antes de volver a intentar, si el header
/// <c>Retry-After</c> lo informó.
/// </summary>
public class AnilistRateLimitException : AnilistApiException
{
    public TimeSpan? RetryAfter { get; }

    public AnilistRateLimitException(TimeSpan? retryAfter, Exception innerException)
        : base(BuildMessage(retryAfter), innerException)
    {
        RetryAfter = retryAfter;
    }

    private static string BuildMessage(TimeSpan? retryAfter) =>
        retryAfter is { } ra
            ? $"AniList alcanzó el rate limit. Reintentar en {ra.TotalSeconds:0}s."
            : "AniList alcanzó el rate limit.";
}

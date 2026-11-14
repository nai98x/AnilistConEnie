using System.Net.Http.Json;
using System.Text.Json;
using AnilistConEnie.Domain.Exceptions;
using AnilistConEnie.Domain.Interfaces;

namespace AnilistConEnie.Infrastructure.Anilist;

internal sealed class AnilistOAuthClient(HttpClient http, AnilistOAuthCredenciales credenciales) : IAnilistOAuthClient
{
    private const string Endpoint = "https://anilist.co/api/v2/oauth/token";

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<string> CanjearCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var cuerpo = new
        {
            GrantType = "authorization_code",
            credenciales.ClientId,
            credenciales.ClientSecret,
            credenciales.RedirectUri,
            Code = codigo
        };

        using HttpRequestMessage request = new(HttpMethod.Post, Endpoint);
        request.Headers.Accept.ParseAdd("application/json");
        request.Content = JsonContent.Create(cuerpo, options: Json);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new AnilistApiException("No se pudo conectar con AniList.", ex);
        }

        using (response)
        {
            if ((int)response.StatusCode >= 500)
                throw new AnilistServerErrorException((int)response.StatusCode, new HttpRequestException(null, null, response.StatusCode));

            if (!response.IsSuccessStatusCode)
                throw new AnilistApiException($"AniList rechazó el código de autorización (HTTP {(int)response.StatusCode}).");

            TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, cancellationToken);
            return token?.AccessToken is { Length: > 0 } accessToken
                ? accessToken
                : throw new AnilistApiException("AniList no devolvió un token de acceso.");
        }
    }

    private sealed record TokenResponse(string? AccessToken);
}

namespace AnilistConEnie.Domain.Interfaces;

public interface IAnilistOAuthClient
{
    /// <summary>
    /// Canjea el código que AniList entrega en la redirect URI (flujo Authorization Code) por el
    /// token de acceso del usuario.
    /// </summary>
    Task<string> CanjearCodigoAsync(string codigo, CancellationToken cancellationToken = default);
}

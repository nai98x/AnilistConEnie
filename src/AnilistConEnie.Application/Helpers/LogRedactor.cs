namespace AnilistConEnie.Application.Helpers;

public static class LogRedactor
{
    private static readonly string[] NombresSensibles = ["token", "password", "contraseña", "contrasena", "clave", "secret", "key"];

    public static string RedactarValor(string nombreParametro, string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return valor ?? string.Empty;

        return NombresSensibles.Any(s => nombreParametro.Contains(s, StringComparison.OrdinalIgnoreCase))
            ? "***"
            : valor;
    }
}

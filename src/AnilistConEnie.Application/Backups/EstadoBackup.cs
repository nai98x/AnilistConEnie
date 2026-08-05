using System.Globalization;

namespace AnilistConEnie.Application.Backups;

public static class EstadoBackup
{
    public static bool EstaAlDia(string? marca, DateOnly hoy) =>
        TryParseMarca(marca, out DateOnly fecha) && fecha >= hoy;

    public static string DescribirUltimo(string? marca) =>
        TryParseMarca(marca, out DateOnly fecha) ? fecha.ToString("yyyy-MM-dd") : "desconocido";

    private static bool TryParseMarca(string? marca, out DateOnly fecha) =>
        DateOnly.TryParseExact(marca?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
}

using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Xp;

public enum XpRangoParcial
{
    Diario,
    Semanal,
    Mensual,
    Anual
}

public static class XpRankingParcial
{
    /// <summary>
    /// Primer día del período calendario en curso: Diario = hoy, Semanal = lunes de esta semana,
    /// Mensual = 1° del mes, Anual = 1 de enero.
    /// </summary>
    public static DateOnly InicioRango(XpRangoParcial rango, DateOnly hoy) => rango switch
    {
        XpRangoParcial.Diario => hoy,
        XpRangoParcial.Semanal => hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)),
        XpRangoParcial.Mensual => new DateOnly(hoy.Year, hoy.Month, 1),
        _ => new DateOnly(hoy.Year, 1, 1)
    };

    /// <summary>
    /// Ranking del XP ganado en el período: total actual menos el baseline (acumulado al inicio del
    /// período; 0 si el usuario no tiene registro previo). Incluye a todos, igual que el ranking
    /// Total; solo descarta deltas negativos (anomalía de datos).
    /// </summary>
    public static IReadOnlyList<XpRankEntry> Build(
        IReadOnlyList<UserXp> totales,
        IReadOnlyDictionary<long, long> baselines)
    {
        List<XpRankEntry> result = [];
        int rank = 0;
        foreach ((long userId, long delta) in totales
                     .Select(x => (x.UserId, Delta: x.Total - baselines.GetValueOrDefault(x.UserId)))
                     .Where(x => x.Delta >= 0)
                     .OrderByDescending(x => x.Delta))
            result.Add(new XpRankEntry(++rank, delta, (ulong)userId));

        return result;
    }
}

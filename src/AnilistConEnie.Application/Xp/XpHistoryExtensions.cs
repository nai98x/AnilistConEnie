using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Xp;

public static class XpHistoryExtensions
{
    /// <summary>
    /// Promedio de XP ganada por día: diferencia de XP entre el primer y el último registro dividida
    /// por los días transcurridos entre ambos. Devuelve 0 si no se puede calcular.
    /// </summary>
    public static double GetPromedio(this IReadOnlyList<UserDailyXp> registros)
    {
        if (registros.Count == 0) return 0;

        UserDailyXp first = registros[0];
        UserDailyXp last = registros[^1];

        double totalDays = (last.Date - first.Date).TotalDays;
        if (totalDays <= 0) return 0;

        return (last.Xp - first.Xp) / totalDays;
    }

    /// <summary>
    /// Mismo promedio que <see cref="GetPromedio(IReadOnlyList{UserDailyXp})"/> sobre el historial del chart,
    /// a partir solo del primer y el último registro: si el último es del año en curso, el chart lo
    /// extiende hasta hoy con la misma XP.
    /// </summary>
    public static double GetPromedio(this XpDiarioExtremos extremos, DateTime hoy)
    {
        DateTime hasta = extremos.UltimaFecha.Year == hoy.Year ? hoy.Date : extremos.UltimaFecha;

        double totalDays = (hasta - extremos.PrimeraFecha).TotalDays;
        if (totalDays <= 0) return 0;

        return (extremos.UltimoXp - extremos.PrimerXp) / totalDays;
    }
}

using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;

namespace AnilistConEnie.Application.Xp;

/// <summary>
/// Arma el historial diario de XP por usuario leyendo del repositorio relacional en cada llamada
/// y persistiendo los días faltantes que rellena <see cref="XpChartBuilder"/>.
/// </summary>
public class XpChartService(IXpDiarioRepository xpDiarioRepository)
{
    public async Task<List<UserDailyXp>> GetUserChartHistory(
        ulong userId,
        bool includeZeroXp = false,
        bool rellenarDiasFaltantes = true)
    {
        List<UserDailyXp> historial = await xpDiarioRepository.ObtenerChart(userId);

        XpChartResult chart = XpChartBuilder.Build((long)userId, historial, DateTime.Today, includeZeroXp, rellenarDiasFaltantes);

        foreach (UserDailyXp day in chart.MissingDaysToPersist)
            _ = xpDiarioRepository.Upsert(userId, day.Date, day.Xp);

        return [..chart.Points];
    }

    /// <summary>
    /// Historial semanal (1 punto cada 7 días) de los últimos 365 días, para el comparativo de
    /// "/topchart". No rellena/persiste días individuales (a diferencia de <see cref="GetUserChartHistory"/>):
    /// las fechas de muestreo son las mismas para todos los usuarios comparados.
    /// </summary>
    public async Task<List<UserDailyXp>> GetUserWeeklyHistory(ulong userId, long currentXp)
    {
        List<UserDailyXp> historial = await xpDiarioRepository.ObtenerChart(userId);
        return XpTopChartHistory.Build((long)userId, historial, DateTime.Today, currentXp);
    }
}

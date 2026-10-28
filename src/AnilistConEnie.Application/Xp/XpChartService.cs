using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;

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

        XpChartResult chart = XpChartBuilder.Build((long)userId, historial, RelojServidor.Hoy, includeZeroXp, rellenarDiasFaltantes);

        if (chart.MissingDaysToPersist.Count > 0)
            await xpDiarioRepository.InsertBulk(userId, chart.MissingDaysToPersist);

        return [..chart.Points];
    }

    /// <summary>
    /// Historial muestreado según <paramref name="periodo"/> para el comparativo de "/topchart". No
    /// rellena/persiste días individuales (a diferencia de <see cref="GetUserChartHistory"/>): las
    /// fechas de muestreo son las mismas para todos los usuarios comparados.
    /// </summary>
    public async Task<List<UserDailyXp>> GetUserTopChartHistory(ulong userId, long currentXp, XpTopChartPeriodo periodo)
    {
        List<UserDailyXp> historial = await xpDiarioRepository.ObtenerChart(userId);
        return XpTopChartHistory.Build((long)userId, historial, RelojServidor.Hoy, currentXp, periodo);
    }
}

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
}

using System.Collections.Concurrent;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;

namespace AnilistConEnie.Application.Xp;

/// <summary>
/// Cachea el historial diario de XP por usuario y lo sincroniza con el repositorio: carga read-through
/// (lazy) y persiste los días faltantes que rellena <see cref="XpChartBuilder"/>.
/// </summary>
public class XpChartService(IUsuariosDiscordRepository usuariosDiscordRepository)
{
    private readonly ConcurrentDictionary<ulong, List<UserDailyXp>> _dailyXp = new();

    public async Task AddUserXpToChartHistory(ulong userId, long xpFromDay, DateTime date)
    {
        if (_dailyXp.TryGetValue(userId, out var xp))
        {
            xp.Add(new UserDailyXp { Date = date, UserId = (long)userId, Xp = xpFromDay });
        }
        else
        {
            xp = await usuariosDiscordRepository.GetDailyXpChartFromUser(userId, DateRangeXp.Anual);
            _dailyXp[userId] = xp;
        }
    }

    public async Task<List<UserDailyXp>> GetUserChartHistory(
        ulong userId,
        bool includeZeroXp = false,
        bool rellenarDiasFaltantes = true)
    {
        if (!_dailyXp.TryGetValue(userId, out var listTmp))
        {
            listTmp = await usuariosDiscordRepository.GetDailyXpChartFromUser(userId, DateRangeXp.Anual);
            _dailyXp[userId] = listTmp;
        }

        XpChartResult chart = XpChartBuilder.Build((long)userId, listTmp, DateTime.Today, includeZeroXp, rellenarDiasFaltantes);

        if (chart.MissingDaysToPersist.Count > 0)
        {
            _dailyXp[userId] = [..chart.Points];
            foreach (UserDailyXp day in chart.MissingDaysToPersist)
                _ = usuariosDiscordRepository.AddDailyXp(day.Date, userId, day.Xp);
        }

        return [..chart.Points];
    }

    public void ResetXpChartHistory() => _dailyXp.Clear();
}

using AnilistConEnie.Application.Confessions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Configuration;

namespace AnilistConEnie.Bot.Services.State;

public class ConfessionsState(ConfesionesSettings settings)
{
    // Los eventos de Discord se despachan concurrentemente (reacciones simultáneas, /fun confesion),
    // y las operaciones tocan varios dicts como unidad: se serializa todo con un lock.
    private readonly object _lock = new();
    private readonly Dictionary<ulong, ulong> _dailyConfessionUsers = new();
    private readonly Dictionary<ulong, ulong> _dailyConfessedMessages = new();
    private readonly Dictionary<ulong, List<ulong>> _confessions = new();

    public void ResetDailyConfessions()
    {
        lock (_lock)
        {
            _dailyConfessionUsers.Clear();
            _confessions.Clear();
            _dailyConfessedMessages.Clear();
        }
    }

    public void AddDailyConfessionUser(ulong userId, ulong messageId)
    {
        lock (_lock)
        {
            if (_dailyConfessionUsers.TryAdd(userId, messageId))
                _confessions.Add(userId, []);
        }
    }

    public bool UserConfessed(ulong userId)
    {
        lock (_lock) return _dailyConfessionUsers.ContainsKey(userId);
    }

    public bool MessageConfessionGuessed(ulong messageId)
    {
        lock (_lock) return _dailyConfessedMessages.ContainsKey(messageId);
    }

    public bool IsConfession(ulong messageId)
    {
        lock (_lock) return _dailyConfessionUsers.ContainsValue(messageId);
    }

    public (bool, ulong, ulong) AddConfessionReaction(ulong messageId, ulong userReactedId)
    {
        lock (_lock)
        {
            if (_dailyConfessedMessages.ContainsKey(messageId)) return (false, 0, 0);

            var confessionUser = _dailyConfessionUsers.First(x => x.Value == messageId);
            var confessionReactions = _confessions[confessionUser.Key];

            if (confessionReactions.Contains(userReactedId) || userReactedId == confessionUser.Key) return (false, 0, 0);

            confessionReactions.Add(userReactedId);

            int revealPercentage = ConfessionRevealPolicy.RevealChancePercent(confessionReactions.Count, settings.PorcentajePorReaccion);
            if (NumberHelper.GetNumeroRandom(0, 100) > revealPercentage) return (false, 0, 0);

            _dailyConfessedMessages.Add(confessionUser.Value, confessionUser.Key);
            return (true, confessionUser.Value, confessionUser.Key);
        }
    }
}

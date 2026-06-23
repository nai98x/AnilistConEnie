using AnilistConEnie.Application.Confessions;
using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Bot.Services.State;

public class ConfessionsState
{
    // Las confesiones se resetean juntas como unidad y tienen operaciones multi-dict; sin acceso concurrente real
    private readonly Dictionary<ulong, ulong> _dailyConfessionUsers = new();
    private readonly Dictionary<ulong, ulong> _dailyConfessedMessages = new();
    private readonly Dictionary<ulong, List<ulong>> _confessions = new();

    public void ResetDailyConfessions()
    {
        _dailyConfessionUsers.Clear();
        _confessions.Clear();
        _dailyConfessedMessages.Clear();
    }

    public void AddDailyConfessionUser(ulong userId, ulong messageId)
    {
        if (_dailyConfessionUsers.TryAdd(userId, messageId))
            _confessions.Add(userId, []);
    }

    public bool UserConfessed(ulong userId) => _dailyConfessionUsers.ContainsKey(userId);

    public bool MessageConfessionGuessed(ulong messageId) => _dailyConfessedMessages.ContainsKey(messageId);

    public bool IsConfession(ulong messageId) => _dailyConfessionUsers.ContainsValue(messageId);

    public (bool, ulong, ulong) AddConfessionReaction(ulong messageId, ulong userReactedId)
    {
        var confessionUser = _dailyConfessionUsers.First(x => x.Value == messageId);
        var confessionReactions = _confessions[confessionUser.Key];

        if (confessionReactions.Contains(userReactedId) || userReactedId == confessionUser.Key) return (false, 0, 0);

        confessionReactions.Add(userReactedId);
        _confessions[confessionUser.Key] = confessionReactions;

        int revealPercentage = ConfessionRevealPolicy.RevealChancePercent(confessionReactions.Count);
        if (NumberHelper.GetNumeroRandom(0, 100) > revealPercentage) return (false, 0, 0);

        _dailyConfessedMessages.Add(confessionUser.Value, confessionUser.Key);
        return (true, confessionUser.Value, confessionUser.Key);
    }
}

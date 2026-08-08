using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class XpState
{
    private List<ulong> _boosters = [];
    private (bool, ulong) _debugXp = (false, 0);

    private readonly ConcurrentDictionary<ulong, DateTime> _xpCooldowns = new();

    #region Boosters extra xp
    public void FillBoosters(List<ulong> users) => _boosters = users;
    public List<ulong> GetBoosters() => _boosters;
    #endregion

    #region Cooldown de xp por mensaje
    public bool TryClaimXp(ulong userId, TimeSpan cooldown)
    {
        DateTime ahora = DateTime.UtcNow;
        while (true)
        {
            if (_xpCooldowns.TryGetValue(userId, out DateTime ultimo))
            {
                if (ahora - ultimo < cooldown) return false;
                if (_xpCooldowns.TryUpdate(userId, ahora, ultimo)) return true;
            }
            else if (_xpCooldowns.TryAdd(userId, ahora)) return true;
        }
    }
    #endregion

    #region Debug XP
    public (bool, ulong) GetDebugXp() => _debugXp;
    public void EnableDebugXp(ulong id) => _debugXp = (true, id);
    public void DisableDebugXp() => _debugXp = (false, 0);
    #endregion
}

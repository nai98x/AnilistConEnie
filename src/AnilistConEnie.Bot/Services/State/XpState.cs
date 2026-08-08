using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;
using DSharpPlus.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class XpState
{
    private List<ulong> _boosters = [];
    private (bool, ulong) _debugXp = (false, 0);

    private readonly ConcurrentDictionary<ulong, DateTime> _xpCooldowns = new();
    private readonly ConcurrentDictionary<ulong, UserXp> _generalXp = new();

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

    #region Cache de xp del servidor
    public void FillGuildXp(Dictionary<ulong, UserXp> users)
    {
        _generalXp.Clear();
        foreach (var (key, value) in users)
            _generalXp[key] = value;
    }

    public void UpdateUserXp(ulong userId, long xp, TipoXp tipo)
    {
        if (!_generalXp.TryGetValue(userId, out var value))
        {
            value = new UserXp { UserId = (long)userId };
            _generalXp[userId] = value;
        }

        switch (tipo)
        {
            case TipoXp.Total: value.Total = xp; break;
            case TipoXp.Booster: value.Booster = xp; break;
            case TipoXp.Challenges: value.Challenges = xp; break;
            case TipoXp.Eventos: value.Eventos = xp; break;
            case TipoXp.Intercambios: value.Intercambios = xp; break;
            default: value.Otros = xp; break;
        }

        _generalXp[userId] = value;
    }

    public void SetUserXp(ulong userId, UserXp xp)
    {
        xp.UserId = (long)userId;
        _generalXp[userId] = xp;
    }

    public List<UserXp> GetGuildXp(DiscordGuild guild) =>
        _generalXp.Where(x => guild.Members.ContainsKey(x.Key)).Select(x => x.Value).ToList();

    public UserXp GetUserXp(ulong userId) =>
        _generalXp.TryGetValue(userId, out var xp) ? xp : new UserXp();
    #endregion

    #region Debug XP
    public (bool, ulong) GetDebugXp() => _debugXp;
    public void EnableDebugXp(ulong id) => _debugXp = (true, id);
    public void DisableDebugXp() => _debugXp = (false, 0);
    #endregion
}

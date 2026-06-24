using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using DSharpPlus.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class XpState
{
    private List<ulong> _boosters = [];
    private (bool, ulong) _debugXp = (false, 0);

    private readonly ConcurrentDictionary<ulong, bool> _usersXp = new();
    private readonly ConcurrentDictionary<ulong, UserXp> _generalXp = new();

    #region Boosters extra xp
    public void FillBoosters(List<ulong> users) => _boosters = users;
    public List<ulong> GetBoosters() => _boosters;
    #endregion

    #region Xp por minuto
    public void AddMemberToObtainXp(ulong userId) => _usersXp.TryAdd(userId, true);
    public void ResetMembersToObtainXp() => _usersXp.Clear();
    public List<ulong> GetMembersToObtainXp() => [.._usersXp.Keys];
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
            _generalXp[userId] = new UserXp { Total = xp, UserId = (long)userId };
            return;
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

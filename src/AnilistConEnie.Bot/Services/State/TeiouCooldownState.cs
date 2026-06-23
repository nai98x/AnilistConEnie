using AnilistConEnie.Model.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class TeiouCooldownState
{
    private readonly ConcurrentDictionary<ulong, DateTime> _teiouNicknameCooldown = new();

    public bool TeiouInCooldown(ulong id) =>
        _teiouNicknameCooldown.TryGetValue(id, out var cd) && cd > DateTime.Now;

    public double GetHoursCooldownTeiou(ulong id) =>
        (_teiouNicknameCooldown[id] - DateTime.Now).TotalHours;

    public void AddTeiouCooldown(ulong id) =>
        _teiouNicknameCooldown[id] = DateTime.Now.AddHours(24);

    public void FillTeiouFromDb(List<TeiouCooldownNickname> list)
    {
        _teiouNicknameCooldown.Clear();
        foreach (var t in list)
        {
            var dt = DateTime.SpecifyKind(t.Cooldown, DateTimeKind.Utc).ToLocalTime();
            _teiouNicknameCooldown[(ulong)t.UserId] = dt;
        }
    }
}

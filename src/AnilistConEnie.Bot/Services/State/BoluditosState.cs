using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class BoluditosState
{
    private readonly ConcurrentDictionary<ulong, bool> _boluditos = new();

    public bool IsBoludito(ulong userId) => _boluditos.ContainsKey(userId);
    public void AddBoludito(ulong userId) => _boluditos.TryAdd(userId, true);
    public void ResetBoluditos() => _boluditos.Clear();
}

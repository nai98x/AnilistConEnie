using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class PermanentUsernameState
{
    private readonly ConcurrentDictionary<ulong, string> _permanentUsernames = new();

    public void SetPermanentUsername(ulong userId, string username) => _permanentUsernames[userId] = username;

    public IReadOnlyDictionary<ulong, string> GetPermanentUsernames() => _permanentUsernames;

    public void RemovePermanentUsername(ulong userId) => _permanentUsernames.TryRemove(userId, out _);
}

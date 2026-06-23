using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class InviteLinkState
{
    private readonly ConcurrentDictionary<ulong, DateTime> _linkRoleUsers = new();

    public IReadOnlyDictionary<ulong, DateTime> GetLinkRoleUsers() => _linkRoleUsers;

    public void AddLinkRoleUser(ulong id, DateTime expiration) => _linkRoleUsers.TryAdd(id, expiration);

    public void RemoveLinkRoleUser(ulong id) => _linkRoleUsers.TryRemove(id, out _);
}

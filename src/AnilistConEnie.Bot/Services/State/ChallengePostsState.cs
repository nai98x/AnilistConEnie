using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

/// <summary>
/// Cache de los replies (ids de usuario de AniList) de cada post de challenge. Consultar AniList por
/// los replies de un post es la parte lenta y limitada por rate limit de los comandos de challenges,
/// y el resultado es el mismo para todos los usuarios, así que se cachea por actividad y se comparte
/// entre <c>/challenges usuario</c> y <c>/challenges ver</c>. Se revalida pasado el TTL porque AniList
/// sigue recibiendo replies con el tiempo.
/// </summary>
public class ChallengePostsState
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<int, CachedReplies> _cache = new();

    public bool TryGet(int activityId, out IReadOnlyList<int> userIds)
    {
        if (_cache.TryGetValue(activityId, out CachedReplies? cached) && DateTimeOffset.UtcNow - cached.FetchedAt < Ttl)
        {
            userIds = cached.UserIds;
            return true;
        }

        userIds = [];
        return false;
    }

    public void Set(int activityId, IReadOnlyList<int> userIds) =>
        _cache[activityId] = new CachedReplies(userIds, DateTimeOffset.UtcNow);

    public void Clear() => _cache.Clear();

    private sealed record CachedReplies(IReadOnlyList<int> UserIds, DateTimeOffset FetchedAt);
}

using AnilistConEnie.Infrastructure.Anilist.Responses;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Exceptions;
using AnilistConEnie.Model.Interfaces;
using GraphQL;

namespace AnilistConEnie.Infrastructure.Anilist;

internal sealed class AnilistClient(AnilistGraphQLExecutor executor) : IAnilistClient
{
    public async Task<AnilistMedia?> GetMediaAsync(int id, CancellationToken cancellationToken = default)
    {
        GraphQLRequest request = new()
        {
            Query = AnilistQueries.MediaById,
            Variables = new { id }
        };

        AnilistResponse<MediaQueryResponse> response = await executor.SendQueryAsync<MediaQueryResponse>(request, cancellationToken);
        MediaDto? media = response.Data?.Media;
        return media is null ? null : MediaMapper.ToDomain(media);
    }

    public async Task<IReadOnlyList<AnilistMedia>> SearchMediaAsync(
        string search,
        AnilistMediaType type,
        int perPage = 5,
        CancellationToken cancellationToken = default)
    {
        GraphQLRequest request = new()
        {
            Query = AnilistQueries.SearchMedia,
            Variables = new { search, type = type.ToString().ToUpperInvariant(), perPage }
        };

        AnilistResponse<MediaSearchResponse> response = await executor.SendQueryAsync<MediaSearchResponse>(request, cancellationToken);
        List<MediaDto>? media = response.Data?.Page?.Media;

        return media is null ? [] : media.Select(MediaMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<AnilistUserScore>> GetMediaUserScoresAsync(
        int mediaId,
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return [];

        GraphQLRequest request = new()
        {
            Query = AnilistQueries.MediaUserScores,
            Variables = new { mediaId, ids = userIds }
        };

        AnilistResponse<MediaScoresResponse> response = await executor.SendQueryAsync<MediaScoresResponse>(request, cancellationToken);
        List<MediaListEntryDto>? entries = response.Data?.Page?.MediaList;

        return entries is null ? [] : entries.Select(MediaMapper.ToUserScore).ToList();
    }

    public async Task<AnilistUser?> SearchUserAsync(string search, CancellationToken cancellationToken = default)
    {
        GraphQLRequest request = new()
        {
            Query = AnilistQueries.UserByName,
            Variables = new { search }
        };

        try
        {
            AnilistResponse<UserSearchResponse> response = await executor.SendQueryAsync<UserSearchResponse>(request, cancellationToken);
            UserDto? user = response.Data?.User;
            return user is null ? null : MediaMapper.ToUser(user);
        }
        // AniList responde con un error GraphQL ("Not Found") cuando ningún usuario coincide.
        catch (AnilistRateLimitException) { throw; }
        catch (AnilistServerErrorException) { throw; }
        catch (AnilistApiException) { return null; }
    }

    public async Task<AnilistViewer?> GetViewerAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        GraphQLRequest request = new() { Query = AnilistQueries.Viewer };

        AnilistResponse<ViewerResponse> response = await executor.SendAuthenticatedQueryAsync<ViewerResponse>(request, accessToken, cancellationToken);
        ViewerDto? viewer = response.Data?.Viewer;
        return viewer is null ? null : MediaMapper.ToViewer(viewer);
    }

    public async Task<AnilistRateLimit> GetRateLimitAsync(CancellationToken cancellationToken = default)
    {
        GraphQLRequest request = new() { Query = AnilistQueries.RateLimitProbe };

        AnilistResponse<MediaQueryResponse> response = await executor.SendQueryAsync<MediaQueryResponse>(request, cancellationToken);
        return response.RateLimit;
    }

    public async Task<IReadOnlyList<int>> GetActivityReplyUserIdsAsync(int activityId, CancellationToken cancellationToken = default)
    {
        GraphQLRequest request = new()
        {
            Query = AnilistQueries.ActivityReplies,
            Variables = new { id = activityId }
        };

        AnilistResponse<ActivityRepliesResponse> response = await executor.SendQueryAsync<ActivityRepliesResponse>(request, cancellationToken);
        List<ActivityReplyDto>? replies = response.Data?.Page?.ActivityReplies;

        return replies is null ? [] : replies.Select(r => r.UserId).ToList();
    }
}

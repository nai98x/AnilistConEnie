using AnilistConEnie.Infrastructure.Anilist.Responses;
using AnilistConEnie.Model.Entities.Anilist;
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

    public async Task<AnilistRateLimit> GetRateLimitAsync(CancellationToken cancellationToken = default)
    {
        GraphQLRequest request = new() { Query = AnilistQueries.RateLimitProbe };

        AnilistResponse<MediaQueryResponse> response = await executor.SendQueryAsync<MediaQueryResponse>(request, cancellationToken);
        return response.RateLimit;
    }
}

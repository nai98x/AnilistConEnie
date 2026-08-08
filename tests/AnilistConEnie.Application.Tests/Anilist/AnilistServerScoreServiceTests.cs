using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Domain.Entities.Anilist;
using AnilistConEnie.Domain.Interfaces;

namespace AnilistConEnie.Application.Tests.Anilist;

public class AnilistServerScoreServiceTests
{
    /// <summary>Fake de IAnilistClient que solo responde GetMediaUserScoresAsync con entradas predefinidas.</summary>
    private sealed class FakeAnilistClient(IReadOnlyList<AnilistUserScore> scores) : IAnilistClient
    {
        public int Batches { get; private set; }

        public Task<IReadOnlyList<AnilistUserScore>> GetMediaUserScoresAsync(
            int mediaId, IReadOnlyCollection<int> userIds, CancellationToken cancellationToken = default)
        {
            Batches++;
            IReadOnlyList<AnilistUserScore> result = scores.Where(s => userIds.Contains(s.UserId)).ToList();
            return Task.FromResult(result);
        }

        // Estos métodos no se usan en estos tests; si alguno se invoca, debe explotar de forma visible.
        private static Exception NoEsperado() => new InvalidOperationException("Método de IAnilistClient no esperado en estos tests");

        public Task<AnilistMedia?> GetMediaAsync(int id, CancellationToken cancellationToken = default) =>
            throw NoEsperado();
        public Task<IReadOnlyList<AnilistMedia>> SearchMediaAsync(string search, AnilistMediaType type, int perPage = 5, CancellationToken cancellationToken = default) =>
            throw NoEsperado();
        public Task<AnilistUser?> SearchUserAsync(string search, CancellationToken cancellationToken = default) =>
            throw NoEsperado();
        public Task<AnilistViewer?> GetViewerAsync(string accessToken, CancellationToken cancellationToken = default) =>
            throw NoEsperado();
        public Task<IReadOnlyList<int>> GetActivityReplyUserIdsAsync(int activityId, CancellationToken cancellationToken = default) =>
            throw NoEsperado();
        public Task<AnilistRateLimit> GetRateLimitAsync(CancellationToken cancellationToken = default) =>
            throw NoEsperado();
    }

    private static AnilistUserScore Score(int userId, double raw, double score100, string status = "COMPLETED") =>
        new() { UserId = userId, RawScore = raw, Score100 = score100, Status = status };

    private static readonly AnilistMedia Media = new() { Id = 1 };

    [Fact]
    public async Task AggregateAsync_NoMembers_ReturnsEmpty()
    {
        FakeAnilistClient client = new([]);
        AnilistServerScoreService service = new(client);

        ServerMediaScores result = await service.AggregateAsync(
            Media, new Dictionary<int, string>(), includeUsersWithoutScore: true);

        Assert.Same(ServerMediaScores.Empty, result);
        Assert.Equal(0, client.Batches);
    }

    [Fact]
    public async Task AggregateAsync_WithScores_AveragesAndClassifies()
    {
        FakeAnilistClient client = new([Score(1, 8, 80), Score(2, 10, 100), Score(3, 0, 0, "CURRENT")]);
        AnilistServerScoreService service = new(client);
        Dictionary<int, string> idToName = new() { [1] = "Ana", [2] = "Beto", [3] = "Cami" };

        ServerMediaScores result = await service.AggregateAsync(Media, idToName, includeUsersWithoutScore: true);

        Assert.Equal(90, result.Average100); // (80 + 100) / 2
        Assert.Equal(2, result.Scored.Count);
        Assert.Single(result.Unscored);
        Assert.Equal("Cami", result.Unscored[0].DisplayName);
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public async Task AggregateAsync_UnscoredNotRequested_ExcludesThem()
    {
        FakeAnilistClient client = new([Score(1, 8, 80), Score(2, 0, 0, "CURRENT")]);
        AnilistServerScoreService service = new(client);
        Dictionary<int, string> idToName = new() { [1] = "Ana", [2] = "Beto" };

        ServerMediaScores result = await service.AggregateAsync(Media, idToName, includeUsersWithoutScore: false);

        Assert.Single(result.Scored);
        Assert.Empty(result.Unscored);
    }

    [Fact]
    public async Task AggregateAsync_PlanningWithoutScore_Ignored()
    {
        FakeAnilistClient client = new([Score(1, 0, 0, "PLANNING")]);
        AnilistServerScoreService service = new(client);
        Dictionary<int, string> idToName = new() { [1] = "Ana" };

        ServerMediaScores result = await service.AggregateAsync(Media, idToName, includeUsersWithoutScore: true);

        Assert.Empty(result.Scored);
        Assert.Empty(result.Unscored);
        Assert.Null(result.Average100);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public async Task AggregateAsync_NoScores_LeavesAverageNull()
    {
        FakeAnilistClient client = new([Score(1, 0, 0, "DROPPED")]);
        AnilistServerScoreService service = new(client);
        Dictionary<int, string> idToName = new() { [1] = "Ana" };

        ServerMediaScores result = await service.AggregateAsync(Media, idToName, includeUsersWithoutScore: true);

        Assert.Null(result.Average100);
        Assert.Single(result.Unscored);
    }

    [Fact]
    public async Task AggregateAsync_ManyUsers_BatchesInChunksOf50()
    {
        // 120 usuarios -> 3 lotes (50 + 50 + 20).
        List<AnilistUserScore> scores = [.. Enumerable.Range(1, 120).Select(i => Score(i, 5, 50))];
        FakeAnilistClient client = new(scores);
        AnilistServerScoreService service = new(client);
        Dictionary<int, string> idToName = Enumerable.Range(1, 120).ToDictionary(i => i, i => $"User{i}");

        ServerMediaScores result = await service.AggregateAsync(Media, idToName, includeUsersWithoutScore: false);

        Assert.Equal(3, client.Batches);
        Assert.Equal(120, result.Scored.Count);
        Assert.Equal(50, result.Average100);
    }
}

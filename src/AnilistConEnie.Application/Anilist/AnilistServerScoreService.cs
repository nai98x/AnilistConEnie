using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Interfaces;

namespace AnilistConEnie.Application.Anilist;

public sealed class AnilistServerScoreService(IAnilistClient anilistClient)
{
    private const int AnilistBatchSize = 50;

    public async Task<ServerMediaScores> AggregateAsync(
        AnilistMedia media,
        IReadOnlyDictionary<int, string> anilistIdToDisplayName,
        bool includeUsersWithoutScore,
        CancellationToken cancellationToken = default)
    {
        if (anilistIdToDisplayName.Count == 0) return ServerMediaScores.Empty;

        List<MemberMediaScore> scored = [];
        List<MemberMediaScore> unscored = [];
        double sum100 = 0;
        int scoredCount = 0;

        foreach (int[] batch in anilistIdToDisplayName.Keys.Chunk(AnilistBatchSize))
        {
            IReadOnlyList<AnilistUserScore> entries =
                await anilistClient.GetMediaUserScoresAsync(media.Id, batch, cancellationToken);

            foreach (AnilistUserScore entry in entries)
            {
                if (!anilistIdToDisplayName.TryGetValue(entry.UserId, out string? displayName))
                    continue;

                if (entry.HasScore)
                {
                    scored.Add(new MemberMediaScore(displayName, entry));
                    sum100 += entry.Score100;
                    scoredCount++;
                }
                else if (includeUsersWithoutScore &&
                         !entry.Status.Equals("PLANNING", StringComparison.OrdinalIgnoreCase))
                {
                    unscored.Add(new MemberMediaScore(displayName, entry));
                }
            }
        }

        double? average = scoredCount > 0 ? sum100 / scoredCount : null;
        return new ServerMediaScores(average, scored, unscored);
    }
}

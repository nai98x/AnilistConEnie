using AnilistConEnie.Model.Entities.Anilist;

namespace AnilistConEnie.Application.Anilist;

public sealed record MemberMediaScore(string DisplayName, AnilistUserScore Entry);

public sealed record ServerMediaScores(
    double? Average100,
    IReadOnlyList<MemberMediaScore> Scored,
    IReadOnlyList<MemberMediaScore> Unscored)
{
    public static ServerMediaScores Empty { get; } = new(null, [], []);

    public bool IsEmpty => Scored.Count == 0 && Unscored.Count == 0;
}

using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpRankingTests
{
    private static UserXp User(long id, long total = 0, long booster = 0, long intercambios = 0,
        long eventos = 0, long challenges = 0, long otros = 0) =>
        new()
        {
            UserId = id,
            Total = total,
            Booster = booster,
            Intercambios = intercambios,
            Eventos = eventos,
            Challenges = challenges,
            Otros = otros
        };

    [Fact]
    public void Build_Total_SortsDescendingIncludesAll()
    {
        List<UserXp> users = [User(1, total: 100), User(2, total: 300), User(3, total: 0)];

        IReadOnlyList<XpRankEntry> ranking = XpRanking.Build(users, XpRankingCategory.Total);

        Assert.Equal(3, ranking.Count);
        Assert.Equal((1, 300L, 2ul), (ranking[0].Rank, ranking[0].Score, ranking[0].UserId));
        Assert.Equal((2, 100L, 1ul), (ranking[1].Rank, ranking[1].Score, ranking[1].UserId));
        Assert.Equal((3, 0L, 3ul), (ranking[2].Rank, ranking[2].Score, ranking[2].UserId));
    }

    [Fact]
    public void Build_NonTotalCategory_ExcludesZeroScores()
    {
        List<UserXp> users = [User(1, intercambios: 50), User(2, intercambios: 0), User(3, intercambios: 200)];

        IReadOnlyList<XpRankEntry> ranking = XpRanking.Build(users, XpRankingCategory.Intercambios);

        Assert.Equal(2, ranking.Count);
        Assert.Equal(3ul, ranking[0].UserId);
        Assert.Equal(1ul, ranking[1].UserId);
        Assert.DoesNotContain(ranking, e => e.UserId == 2);
    }

    [Fact]
    public void Build_Messages_IsTotalMinusOtherCategories()
    {
        // Total 1000, con 100 de cada categoría -> mensajes = 1000 - 500 = 500.
        UserXp u = User(1, total: 1000, booster: 100, intercambios: 100, eventos: 100, challenges: 100, otros: 100);

        IReadOnlyList<XpRankEntry> ranking = XpRanking.Build([u], XpRankingCategory.Mensajes);

        Assert.Single(ranking);
        Assert.Equal(500, ranking[0].Score);
    }

    [Fact]
    public void Build_EmptyList_ReturnsEmptyRanking()
    {
        Assert.Empty(XpRanking.Build([], XpRankingCategory.Total));
    }

    [Fact]
    public void ResolvePosition_AbsentUser_HasNoPosition()
    {
        IReadOnlyList<XpRankEntry> ranking = XpRanking.Build([User(1, total: 100)], XpRankingCategory.Total);

        RankPosition pos = XpRanking.ResolvePosition(ranking, userId: 999);

        Assert.Equal(RankPositionKind.Absent, pos.Kind);
    }

    [Fact]
    public void ResolvePosition_SoleLeader_IsFirst()
    {
        IReadOnlyList<XpRankEntry> ranking = XpRanking.Build([User(1, total: 100)], XpRankingCategory.Total);

        RankPosition pos = XpRanking.ResolvePosition(ranking, userId: 1);

        Assert.Equal(RankPositionKind.SoloLeader, pos.Kind);
        Assert.Equal(1, pos.PositionNumber);
    }

    [Fact]
    public void ResolvePosition_LeaderWithRivalBelow()
    {
        List<UserXp> users = [User(1, total: 300), User(2, total: 100)];
        IReadOnlyList<XpRankEntry> ranking = XpRanking.Build(users, XpRankingCategory.Total);

        RankPosition pos = XpRanking.ResolvePosition(ranking, userId: 1);

        Assert.Equal(RankPositionKind.Leader, pos.Kind);
        Assert.Equal(1, pos.PositionNumber);
        Assert.Equal(200, pos.Diff);
        Assert.Equal(2ul, pos.RivalUserId);
    }

    [Fact]
    public void ResolvePosition_BehindRival()
    {
        List<UserXp> users = [User(1, total: 300), User(2, total: 100)];
        IReadOnlyList<XpRankEntry> ranking = XpRanking.Build(users, XpRankingCategory.Total);

        RankPosition pos = XpRanking.ResolvePosition(ranking, userId: 2);

        Assert.Equal(RankPositionKind.Behind, pos.Kind);
        Assert.Equal(2, pos.PositionNumber);
        Assert.Equal(200, pos.Diff);
        Assert.Equal(1ul, pos.RivalUserId);
    }
}

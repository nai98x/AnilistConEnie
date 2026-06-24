using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Xp;

public enum XpRankingCategory
{
    Total,
    Mensajes,
    Intercambios,
    Eventos,
    Challenges,
    Booster,
    Otros
}

/// <summary>Una posición del ranking de XP ya calculada (rango 1-based, puntaje y usuario).</summary>
public readonly record struct XpRankEntry(int Rank, long Score, ulong UserId);

public enum RankPositionKind
{
    /// <summary>El usuario no participa en este ranking.</summary>
    Absent,
    /// <summary>El usuario es el único del ranking.</summary>
    SoloLeader,
    /// <summary>El usuario lidera y hay alguien detrás (<see cref="RankPosition.RivalUserId"/>).</summary>
    Leader,
    /// <summary>El usuario está por detrás de un rival (<see cref="RankPosition.RivalUserId"/>).</summary>
    Behind
}

/// <summary>
/// Posición del usuario en un ranking ya calculado. <see cref="Diff"/> es la diferencia de XP con el rival
/// relevante (el de arriba si está <see cref="RankPositionKind.Behind"/>, el de abajo si es <see cref="RankPositionKind.Leader"/>).
/// </summary>
public readonly record struct RankPosition(RankPositionKind Kind, int PositionNumber, long Diff, ulong RivalUserId);

public static class XpRanking
{
    /// <summary>
    /// Calcula el ranking del servidor para la categoría indicada, ordenado de mayor a menor puntaje.
    /// La categoría Total incluye a todos; el resto solo a quienes tienen puntaje positivo.
    /// </summary>
    public static IReadOnlyList<XpRankEntry> Build(IReadOnlyList<UserXp> users, XpRankingCategory category)
    {
        Func<UserXp, long> selector = category switch
        {
            XpRankingCategory.Total => x => x.Total,
            XpRankingCategory.Mensajes => x => x.Total - x.Booster - x.Intercambios - x.Challenges - x.Eventos - x.Otros,
            XpRankingCategory.Intercambios => x => x.Intercambios,
            XpRankingCategory.Eventos => x => x.Eventos,
            XpRankingCategory.Challenges => x => x.Challenges,
            XpRankingCategory.Booster => x => x.Booster,
            _ => x => x.Otros
        };

        bool includeZero = category == XpRankingCategory.Total;

        List<XpRankEntry> result = [];
        int rank = 0;
        foreach ((long userId, long score) in users.Select(x => (x.UserId, Score: selector(x))).OrderByDescending(x => x.Score))
            if (includeZero || score > 0)
                result.Add(new XpRankEntry(++rank, score, (ulong)userId));

        return result;
    }

    /// <summary>Ubica al usuario dentro de un ranking ya calculado y describe su posición relativa al rival relevante.</summary>
    public static RankPosition ResolvePosition(IReadOnlyList<XpRankEntry> rankings, ulong userId)
    {
        int index = -1;
        for (int i = 0; i < rankings.Count; i++)
            if (rankings[i].UserId == userId) { index = i; break; }

        if (index < 0)
            return new RankPosition(RankPositionKind.Absent, 0, 0, 0);

        XpRankEntry you = rankings[index];

        if (rankings.Count == 1)
            return new RankPosition(RankPositionKind.SoloLeader, 1, 0, 0);

        if (index > 0)
        {
            XpRankEntry rival = rankings[index - 1];
            return new RankPosition(RankPositionKind.Behind, index + 1, rival.Score - you.Score, rival.UserId);
        }

        XpRankEntry below = rankings[1];
        return new RankPosition(RankPositionKind.Leader, 1, you.Score - below.Score, below.UserId);
    }
}

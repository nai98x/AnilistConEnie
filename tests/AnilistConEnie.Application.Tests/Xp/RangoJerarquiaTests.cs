using AnilistConEnie.Application.Xp;
using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Application.Tests.Xp;

public class RangoJerarquiaTests
{
    [Fact]
    public void IgualOSuperior_MidRank_IncludesRankAndHigher()
    {
        IReadOnlyList<RangoEnum> resultado = RangoJerarquia.IgualOSuperior(RangoEnum.Senpai);

        Assert.Equal(
            new[] { RangoEnum.Senpai, RangoEnum.Hikikomori, RangoEnum.Sensei, RangoEnum.Ousama, RangoEnum.Teiou },
            resultado);
    }

    [Fact]
    public void IgualOSuperior_HighestRank_ReturnsOnlyItself()
    {
        Assert.Equal(new[] { RangoEnum.Teiou }, RangoJerarquia.IgualOSuperior(RangoEnum.Teiou));
    }

    [Fact]
    public void IgualOSuperior_LowestRank_ReturnsAllRanks()
    {
        IReadOnlyList<RangoEnum> resultado = RangoJerarquia.IgualOSuperior(RangoEnum.Tama);

        Assert.Equal(8, resultado.Count);
        Assert.DoesNotContain(RangoEnum.Miembro, resultado);
    }
}

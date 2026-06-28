using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Application.Tests.Xp;

public class RangoXpTests
{
    [Theory]
    [InlineData(RangoEnum.Miembro, 0)]
    [InlineData(RangoEnum.Tama, 2000)]
    [InlineData(RangoEnum.Casual, 10000)]
    [InlineData(RangoEnum.Teiou, 1000000)]
    public void XpRequerida_KnownRank_ReturnsThreshold(RangoEnum rango, long esperado)
    {
        Assert.Equal(esperado, RangoXp.XpRequerida(rango));
    }

    [Fact]
    public void XpRequerida_UnknownRank_ReturnsZero()
    {
        Assert.Equal(0, RangoXp.XpRequerida((RangoEnum)999));
    }

    [Theory]
    [InlineData(0, RangoEnum.Miembro)]
    [InlineData(1999, RangoEnum.Miembro)]
    [InlineData(2000, RangoEnum.Tama)]
    [InlineData(9999, RangoEnum.Tama)]
    [InlineData(10000, RangoEnum.Casual)]
    [InlineData(999999, RangoEnum.Ousama)]
    [InlineData(1000000, RangoEnum.Teiou)]
    [InlineData(5000000, RangoEnum.Teiou)]
    public void RangoActual_Xp_ReturnsHighestNotExceededRank(long xp, RangoEnum esperado)
    {
        Assert.Equal(esperado, RangoXp.RangoActual(xp));
    }

    [Fact]
    public void RangoActual_NegativeXp_ReturnsMiembro()
    {
        Assert.Equal(RangoEnum.Miembro, RangoXp.RangoActual(-100));
    }

    [Theory]
    [InlineData(0, RangoEnum.Tama)]
    [InlineData(1999, RangoEnum.Tama)]
    [InlineData(2000, RangoEnum.Casual)]
    [InlineData(999999, RangoEnum.Teiou)]
    [InlineData(1000000, RangoEnum.Teiou)]
    [InlineData(9999999, RangoEnum.Teiou)]
    public void RangoSiguiente_Xp_ReturnsFirstStrictlyHigherThreshold(long xp, RangoEnum esperado)
    {
        Assert.Equal(esperado, RangoXp.RangoSiguiente(xp));
    }

    [Theory]
    [InlineData(0, RangoEnum.Miembro)]
    [InlineData(2000, RangoEnum.Miembro)]
    [InlineData(2001, RangoEnum.Tama)]
    [InlineData(10000, RangoEnum.Tama)]
    [InlineData(10001, RangoEnum.Casual)]
    [InlineData(1000001, RangoEnum.Teiou)]
    public void RangoAnterior_Xp_ReturnsHighestStrictlyLowerThreshold(long xp, RangoEnum esperado)
    {
        Assert.Equal(esperado, RangoXp.RangoAnterior(xp));
    }

    [Fact]
    public void RangoActualAndRangoSiguiente_AtThreshold_AreConsistent()
    {
        // Justo en el umbral de Casual: el rango actual es Casual y el siguiente Kouhai.
        Assert.Equal(RangoEnum.Casual, RangoXp.RangoActual(10000));
        Assert.Equal(RangoEnum.Kouhai, RangoXp.RangoSiguiente(10000));
    }
}

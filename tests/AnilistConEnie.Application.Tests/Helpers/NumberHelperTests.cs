using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class NumberHelperTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1000, "1.000")]
    [InlineData(1234567, "1.234.567")]
    public void ToSpanish_Int_UsesDotThousandsSeparator(int value, string esperado)
    {
        Assert.Equal(esperado, value.ToSpanish());
    }

    [Fact]
    public void ToSpanish_Long_UsesDotThousandsSeparator()
    {
        Assert.Equal("1.000.000", 1_000_000L.ToSpanish());
    }

    [Fact]
    public void ToSpanish_Double_RoundsWithoutDecimals()
    {
        Assert.Equal("1.235", 1234.6.ToSpanish());
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(95, 100, 0)]
    [InlineData(100, 100, 100)]
    [InlineData(199, 100, 100)]
    [InlineData(200, 100, 200)]
    public void ObtenerMultiploAnterior_RoundsDownToMultiple(long numero, int multiplo, long esperado)
    {
        Assert.Equal(esperado, NumberHelper.ObtenerMultiploAnterior(numero, multiplo));
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(1, 100, 100)]
    [InlineData(100, 100, 100)]
    [InlineData(101, 100, 200)]
    [InlineData(250, 100, 300)]
    public void ObtenerMultiploSiguiente_RoundsUpToMultiple(long numero, int multiplo, long esperado)
    {
        Assert.Equal(esperado, NumberHelper.ObtenerMultiploSiguiente(numero, multiplo));
    }

    [Fact]
    public void GetNumeroRandom_NonPositiveBounds_ReturnsZero()
    {
        Assert.Equal(0, NumberHelper.GetNumeroRandom(-5, 0));
        Assert.Equal(0, NumberHelper.GetNumeroRandom(-5, -1));
    }

    [Fact]
    public void GetNumeroRandom_ValidBounds_ReturnsValueInRange()
    {
        for (int i = 0; i < 100; i++)
        {
            int value = NumberHelper.GetNumeroRandom(10, 20);
            Assert.InRange(value, 10, 19); // Random.Next es exclusivo en el máximo
        }
    }
}

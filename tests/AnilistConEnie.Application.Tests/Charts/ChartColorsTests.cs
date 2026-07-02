using AnilistConEnie.Application.Charts;
using AnilistConEnie.Application.Xp;

namespace AnilistConEnie.Application.Tests.Charts;

public class ChartColorsTests
{
    [Fact]
    public void ForIndex_IsDeterministic()
    {
        Assert.Equal(ChartColors.ForIndex(2), ChartColors.ForIndex(2));
    }

    [Fact]
    public void ForIndex_CyclesThroughPaletteWithoutRepeatingWithinACycle()
    {
        // Antes "toppaises" usaba un color 100% aleatorio por ejecución; ahora el ciclo debe ser
        // estable y no repetir colores dentro de una misma vuelta de la paleta.
        List<string> firstCycle = [.. Enumerable.Range(0, 6).Select(ChartColors.ForIndex)];

        Assert.Equal(6, firstCycle.Distinct().Count());
        Assert.Equal(ChartColors.ForIndex(0), ChartColors.ForIndex(6));
    }

    [Theory]
    [InlineData(XpCategory.Mensajes)]
    [InlineData(XpCategory.Challenges)]
    [InlineData(XpCategory.Eventos)]
    [InlineData(XpCategory.Intercambios)]
    [InlineData(XpCategory.Otros)]
    public void ForXpCategory_CoversEveryCategoryWithAStableColor(XpCategory category)
    {
        (string label, string color, string motivo) = ChartColors.ForXpCategory(category);

        Assert.False(string.IsNullOrWhiteSpace(label));
        Assert.StartsWith("#", color);
        Assert.False(string.IsNullOrWhiteSpace(motivo));
        Assert.Equal(ChartColors.ForXpCategory(category), ChartColors.ForXpCategory(category));
    }
}

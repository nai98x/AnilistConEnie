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
    public void ForIndex_NoRepiteColoresDentroDeLaPaleta()
    {
        List<string> colores = [.. Enumerable.Range(0, ChartColors.Count).Select(ChartColors.ForIndex)];

        Assert.Equal(ChartColors.Count, colores.Distinct().Count());
        Assert.DoesNotContain(ChartColors.Otros, colores);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void ForIndex_FueraDeLaPaleta_Lanza(int index)
    {
        // Más series que colores se agrupan en "Otros"; nunca se recicla un color.
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartColors.ForIndex(index));
    }

    [Fact]
    public void MaxPorcionesTorta_EntraEnLaPaleta()
    {
        Assert.InRange(ChartColors.MaxPorcionesTorta, 1, ChartColors.Count);
    }

    [Fact]
    public void ForXpCategory_OtrosUsaGrisNeutro()
    {
        Assert.Equal(ChartColors.Otros, ChartColors.ForXpCategory(XpCategory.Otros).Color);
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

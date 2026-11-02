using AnilistConEnie.Application.Fun;

namespace AnilistConEnie.Application.Tests.Fun;

public class FloridaManCalculatorTests
{
    [Theory]
    [InlineData("Florida man offers police booze during car chase")]
    [InlineData("Bizarre moment FLORIDA MAN offers deputy vodka")]
    [InlineData("South Florida man points gun at students")]
    public void EsFloridaMan_TitleMentionsFloridaMan_ReturnsTrue(string titulo)
    {
        Assert.True(FloridaManCalculator.EsFloridaMan(titulo));
    }

    [Theory]
    [InlineData("Florida woman poses as ICE agent")]
    [InlineData("Man rescues eagle in Florida")]
    [InlineData("")]
    public void EsFloridaMan_TitleDoesNotMentionFloridaMan_ReturnsFalse(string titulo)
    {
        Assert.False(FloridaManCalculator.EsFloridaMan(titulo));
    }
}

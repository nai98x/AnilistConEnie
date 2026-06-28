using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Application.Tests.Helpers;

public class EnumHelperTests
{
    [Theory]
    [InlineData(Season.Winter, "Winter")]
    [InlineData(Season.Spring, "Spring")]
    [InlineData(Season.Anual, "Anual")]
    public void GetName_Season_ReturnsEnumName(Season season, string esperado)
    {
        Assert.Equal(esperado, season.GetName());
    }

    [Fact]
    public void GetName_InvalidValue_Throws()
    {
        Assert.Throws<Exception>(() => ((Season)999).GetName());
    }
}

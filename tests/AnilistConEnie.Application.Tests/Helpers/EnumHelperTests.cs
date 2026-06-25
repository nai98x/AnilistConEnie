using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Application.Tests.Helpers;

public class EnumHelperTests
{
    [Theory]
    [InlineData(Season.Winter, "Winter")]
    [InlineData(Season.Spring, "Spring")]
    [InlineData(Season.Anual, "Anual")]
    public void GetName_devuelve_el_nombre_del_enum(Season season, string esperado)
    {
        Assert.Equal(esperado, season.GetName());
    }

    [Fact]
    public void GetName_de_valor_invalido_lanza()
    {
        Assert.Throws<Exception>(() => ((Season)999).GetName());
    }
}

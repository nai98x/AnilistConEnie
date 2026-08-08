using System.ComponentModel;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Application.Tests.Extensions;

public class EnumExtensionsTests
{
    private enum SinDescripcion
    {
        Valor
    }

    private enum ConDescripcion
    {
        [Description("Una descripción")]
        Valor
    }

    [Fact]
    public void GetDescription_WithAttribute_ReturnsDescription()
    {
        Assert.Equal("Una descripción", ConDescripcion.Valor.GetDescription());
    }

    [Fact]
    public void GetDescription_WithoutAttribute_ReturnsName()
    {
        Assert.Equal("Valor", SinDescripcion.Valor.GetDescription());
    }

    [Fact]
    public void GetDescription_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ((Enum?)null).GetDescription());
    }

    [Fact]
    public void GetDescription_DomainRangoEnum_ReturnsDescription()
    {
        Assert.Equal("Hikikomori", RangoEnum.Hikikomori.GetDescription());
    }
}

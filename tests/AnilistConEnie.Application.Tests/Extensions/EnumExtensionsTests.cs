using System.ComponentModel;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Model.Enum;

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
    public void GetDescription_lee_el_atributo_description()
    {
        Assert.Equal("Una descripción", ConDescripcion.Valor.GetDescription());
    }

    [Fact]
    public void GetDescription_sin_atributo_devuelve_el_nombre()
    {
        Assert.Equal("Valor", SinDescripcion.Valor.GetDescription());
    }

    [Fact]
    public void GetDescription_nulo_devuelve_vacio()
    {
        Assert.Equal(string.Empty, ((Enum?)null).GetDescription());
    }

    [Fact]
    public void GetDescription_funciona_con_RangoEnum_del_dominio()
    {
        Assert.Equal("Hikikomori", RangoEnum.Hikikomori.GetDescription());
    }
}

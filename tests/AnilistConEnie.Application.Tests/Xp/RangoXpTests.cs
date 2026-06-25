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
    public void XpRequerida_devuelve_el_umbral_del_rango(RangoEnum rango, long esperado)
    {
        Assert.Equal(esperado, RangoXp.XpRequerida(rango));
    }

    [Fact]
    public void XpRequerida_devuelve_cero_para_rango_desconocido()
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
    public void RangoActual_es_el_mayor_umbral_no_superado(long xp, RangoEnum esperado)
    {
        Assert.Equal(esperado, RangoXp.RangoActual(xp));
    }

    [Fact]
    public void RangoActual_con_xp_negativa_devuelve_Miembro()
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
    public void RangoSiguiente_es_el_primer_umbral_estrictamente_mayor(long xp, RangoEnum esperado)
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
    public void RangoAnterior_es_el_mayor_umbral_estrictamente_menor(long xp, RangoEnum esperado)
    {
        Assert.Equal(esperado, RangoXp.RangoAnterior(xp));
    }

    [Fact]
    public void RangoActual_y_RangoSiguiente_son_consistentes_en_un_umbral()
    {
        // Justo en el umbral de Casual: el rango actual es Casual y el siguiente Kouhai.
        Assert.Equal(RangoEnum.Casual, RangoXp.RangoActual(10000));
        Assert.Equal(RangoEnum.Kouhai, RangoXp.RangoSiguiente(10000));
    }
}

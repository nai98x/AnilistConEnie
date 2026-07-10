using AnilistConEnie.Application.Membership;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Membership;

public class CumpleCalculatorTests
{
    private static Usuario Cumple(long userId, int dia, int mes) =>
        new() { UserId = userId, CumpleDia = (short)dia, CumpleMes = (short)mes };

    [Theory]
    [InlineData(1, 1)]
    [InlineData(31, 12)]
    [InlineData(29, 2)]
    [InlineData(30, 6)]
    public void EsFechaValida_FechasReales_ReturnsTrue(int dia, int mes)
    {
        Assert.True(CumpleCalculator.EsFechaValida(dia, mes));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(32, 1)]
    [InlineData(31, 2)]
    [InlineData(30, 2)]
    [InlineData(31, 4)]
    [InlineData(15, 0)]
    [InlineData(15, 13)]
    [InlineData(99, 99)]
    public void EsFechaValida_FechasInvalidas_ReturnsFalse(int dia, int mes)
    {
        Assert.False(CumpleCalculator.EsFechaValida(dia, mes));
    }

    [Theory]
    [InlineData(5, 7, true)]
    [InlineData(6, 7, false)]
    [InlineData(5, 8, false)]
    public void EsDelDia_ComparaDiaYMes(int dia, int mes, bool esperado)
    {
        Assert.Equal(esperado, CumpleCalculator.EsDelDia(Cumple(1, dia, mes), new DateTime(2026, 7, 5)));
    }

    [Fact]
    public void Proximos_29DeFebrero_NoLanzaEnAnioNoBisiesto()
    {
        List<Usuario> cumples = [Cumple(1, 29, 2)];

        List<UserCumple> lista = CumpleCalculator.Proximos(cumples, new DateTime(2026, 1, 15), soloMes: false);

        Assert.Single(lista);
        Assert.Equal(28, lista[0].Birthday.Day);
        Assert.Equal(2, lista[0].Birthday.Month);
    }

    [Fact]
    public void Proximos_FechaInvalidaPersistida_SeIgnoraSinLanzar()
    {
        List<Usuario> cumples = [Cumple(1, 31, 2), Cumple(2, 10, 3)];

        List<UserCumple> lista = CumpleCalculator.Proximos(cumples, new DateTime(2026, 1, 15), soloMes: false);

        Assert.Single(lista);
        Assert.Equal(2, lista[0].Id);
    }

    [Fact]
    public void Proximos_SoloMes_FiltraFueraDelMes()
    {
        List<Usuario> cumples = [Cumple(1, 20, 1), Cumple(2, 20, 6)];

        List<UserCumple> lista = CumpleCalculator.Proximos(cumples, new DateTime(2026, 1, 15), soloMes: true);

        Assert.Single(lista);
        Assert.Equal(1, lista[0].Id);
    }
}

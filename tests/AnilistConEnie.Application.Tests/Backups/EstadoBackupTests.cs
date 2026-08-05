using AnilistConEnie.Application.Backups;

namespace AnilistConEnie.Application.Tests.Backups;

public class EstadoBackupTests
{
    private static readonly DateOnly Hoy = new(2026, 8, 5);

    [Fact]
    public void EstaAlDia_MarcaDeHoy_EsVerdadero()
    {
        Assert.True(EstadoBackup.EstaAlDia("2026-08-05", Hoy));
    }

    [Fact]
    public void EstaAlDia_MarcaConSaltoDeLinea_EsVerdadero()
    {
        Assert.True(EstadoBackup.EstaAlDia("2026-08-05\n", Hoy));
    }

    [Fact]
    public void EstaAlDia_MarcaDeAyer_EsFalso()
    {
        Assert.False(EstadoBackup.EstaAlDia("2026-08-04", Hoy));
    }

    [Fact]
    public void EstaAlDia_MarcaFutura_EsVerdadero()
    {
        Assert.True(EstadoBackup.EstaAlDia("2026-08-06", Hoy));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("cualquier cosa")]
    [InlineData("05/08/2026")]
    public void EstaAlDia_MarcaAusenteOInvalida_EsFalso(string? marca)
    {
        Assert.False(EstadoBackup.EstaAlDia(marca, Hoy));
    }

    [Fact]
    public void DescribirUltimo_MarcaValida_DevuelveLaFecha()
    {
        Assert.Equal("2026-08-04", EstadoBackup.DescribirUltimo("2026-08-04\n"));
    }

    [Fact]
    public void DescribirUltimo_SinMarca_DevuelveDesconocido()
    {
        Assert.Equal("desconocido", EstadoBackup.DescribirUltimo(null));
    }
}

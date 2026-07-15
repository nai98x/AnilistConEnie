using AnilistConEnie.Application.Membership;

namespace AnilistConEnie.Application.Tests.Membership;

public class InactividadCalculatorTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MiembroNuevoSinActividad_NoCorresponde()
    {
        DateTimeOffset entrada = Ahora.AddDays(-2);

        Assert.False(InactividadCalculator.CorrespondeRolInactivo(entrada, false, Ahora, 3));
    }

    [Fact]
    public void MiembroDentroDeLaVentanaSinActividad_NoCorresponde()
    {
        DateTimeOffset entrada = Ahora.AddMonths(-3).AddDays(1);

        Assert.False(InactividadCalculator.CorrespondeRolInactivo(entrada, false, Ahora, 3));
    }

    [Fact]
    public void MiembroViejoSinActividad_Corresponde()
    {
        DateTimeOffset entrada = Ahora.AddMonths(-6);

        Assert.True(InactividadCalculator.CorrespondeRolInactivo(entrada, false, Ahora, 3));
    }

    [Fact]
    public void MiembroViejoConActividadReciente_NoCorresponde()
    {
        DateTimeOffset entrada = Ahora.AddYears(-2);

        Assert.False(InactividadCalculator.CorrespondeRolInactivo(entrada, true, Ahora, 3));
    }

    [Fact]
    public void MiembroQueEntroJustoEnElLimite_Corresponde()
    {
        DateTimeOffset entrada = Ahora.AddMonths(-3);

        Assert.True(InactividadCalculator.CorrespondeRolInactivo(entrada, false, Ahora, 3));
    }
}

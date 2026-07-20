using AnilistConEnie.Application.Fun;

namespace AnilistConEnie.Application.Tests.Fun;

public class BoludometroCalculatorTests
{
    [Fact]
    public void Seed_MismoUsuarioYMes_EsEstable()
    {
        Assert.Equal(BoludometroCalculator.Seed(123, 2026, 7), BoludometroCalculator.Seed(123, 2026, 7));
    }

    [Fact]
    public void Seed_CambiaDeMes_CambiaLaSemilla()
    {
        Assert.NotEqual(BoludometroCalculator.Seed(123, 2026, 7), BoludometroCalculator.Seed(123, 2026, 8));
    }

    [Fact]
    public void GenerarHistorial_MismaSemilla_ProduceMismoResultado()
    {
        Dictionary<int, int> a = BoludometroCalculator.GenerarHistorial(new Random(42), 15, 31);
        Dictionary<int, int> b = BoludometroCalculator.GenerarHistorial(new Random(42), 15, 31);

        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData(1, 31)]
    [InlineData(15, 31)]
    [InlineData(28, 28)]
    [InlineData(30, 30)]
    public void GenerarHistorial_TieneUnaEntradaPorDiaHastaHoy(int diaHoy, int totalDiasDelMes)
    {
        Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorial(new Random(7), diaHoy, totalDiasDelMes);

        Assert.Equal(diaHoy, historial.Count);
        Assert.Equal(Enumerable.Range(1, diaHoy), historial.Keys.OrderBy(k => k));
    }

    [Fact]
    public void GenerarHistorial_ValoresDentroDe0A100()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorial(new Random(seed), 20, 30);
            Assert.All(historial.Values, v => Assert.InRange(v, 0, 100));
        }
    }

    [Fact]
    public void GenerarHistorial_UltimoDiaEsHoy()
    {
        Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorial(new Random(99), 12, 31);
        Assert.Equal(12, historial.Last().Key);
    }
}

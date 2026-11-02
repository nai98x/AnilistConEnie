using AnilistConEnie.Application.Fun;

namespace AnilistConEnie.Application.Tests.Fun;

public class BoludometroCalculatorTests
{
    [Fact]
    public void Seed_SameUserAndMonth_IsStable()
    {
        Assert.Equal(BoludometroCalculator.Seed(123, 2026, 7), BoludometroCalculator.Seed(123, 2026, 7));
    }

    [Fact]
    public void Seed_DifferentMonth_ChangesSeed()
    {
        Assert.NotEqual(BoludometroCalculator.Seed(123, 2026, 7), BoludometroCalculator.Seed(123, 2026, 8));
    }

    [Fact]
    public void GenerarHistorial_SameSeed_ProducesSameResult()
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
    public void GenerarHistorial_AnyDay_HasOneEntryPerDayUntilToday(int diaHoy, int totalDiasDelMes)
    {
        Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorial(new Random(7), diaHoy, totalDiasDelMes);

        Assert.Equal(diaHoy, historial.Count);
        Assert.Equal(Enumerable.Range(1, diaHoy), historial.Keys.OrderBy(k => k));
    }

    [Fact]
    public void GenerarHistorial_AnySeed_ValuesWithin0To100()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorial(new Random(seed), 20, 30);
            Assert.All(historial.Values, v => Assert.InRange(v, 0, 100));
        }
    }

    [Fact]
    public void GenerarHistorial_AnyDay_LastDayIsToday()
    {
        Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorial(new Random(99), 12, 31);
        Assert.Equal(12, historial.Last().Key);
    }

    [Fact]
    public void GenerarHistorialAnual_SameInput_ProducesSameResult()
    {
        Dictionary<int, int> a = BoludometroCalculator.GenerarHistorialAnual(123, 2026, 11, 2);
        Dictionary<int, int> b = BoludometroCalculator.GenerarHistorialAnual(123, 2026, 11, 2);

        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 28)]
    [InlineData(11, 2)]
    [InlineData(12, 31)]
    public void GenerarHistorialAnual_AnyMonth_HasOneEntryPerMonthUntilCurrent(int mesActual, int diaHoy)
    {
        Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorialAnual(123, 2026, mesActual, diaHoy);

        Assert.Equal(Enumerable.Range(1, mesActual), historial.Keys.OrderBy(k => k));
    }

    [Fact]
    public void GenerarHistorialAnual_AnyUser_ValuesWithin0To100()
    {
        for (ulong userId = 0; userId < 50; userId++)
        {
            Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorialAnual(userId, 2026, 12, 31);
            Assert.All(historial.Values, v => Assert.InRange(v, 0, 100));
        }
    }

    [Fact]
    public void GenerarHistorialAnual_PastMonth_MatchesFullMonthlyAverage()
    {
        Dictionary<int, int> mensual = BoludometroCalculator.GenerarHistorial(
            new Random(BoludometroCalculator.Seed(123, 2026, 3)), 31, 31);

        Dictionary<int, int> anual = BoludometroCalculator.GenerarHistorialAnual(123, 2026, 11, 2);

        Assert.Equal((int)Math.Round(mensual.Values.Average(), MidpointRounding.AwayFromZero), anual[3]);
    }

    [Fact]
    public void GenerarHistorialAnual_CurrentMonth_AveragesOnlyUntilToday()
    {
        Dictionary<int, int> mensual = BoludometroCalculator.GenerarHistorial(
            new Random(BoludometroCalculator.Seed(123, 2026, 11)), 2, 30);

        Dictionary<int, int> anual = BoludometroCalculator.GenerarHistorialAnual(123, 2026, 11, 2);

        Assert.Equal((int)Math.Round(mensual.Values.Average(), MidpointRounding.AwayFromZero), anual[11]);
    }

    [Fact]
    public void ValorDelDia_AnyDay_MatchesMonthlyHistory()
    {
        Dictionary<int, int> historial = BoludometroCalculator.GenerarHistorial(
            new Random(BoludometroCalculator.Seed(123, 2026, 11)), 15, 30);

        Assert.Equal(historial[15], BoludometroCalculator.ValorDelDia(123, 2026, 11, 15));
    }

    [Fact]
    public void BoluditoDelDia_NoCandidates_ReturnsNull()
    {
        Assert.Null(BoludometroCalculator.BoluditoDelDia([], 2026, 11, 2));
    }

    [Fact]
    public void BoluditoDelDia_ManyCandidates_ReturnsHighestValue()
    {
        List<ulong> candidatos = [.. Enumerable.Range(1, 200).Select(i => (ulong)i)];

        (ulong UserId, int Valor)? boludito = BoludometroCalculator.BoluditoDelDia(candidatos, 2026, 11, 2);

        Assert.NotNull(boludito);
        Assert.Equal(candidatos.Max(id => BoludometroCalculator.ValorDelDia(id, 2026, 11, 2)), boludito.Value.Valor);
        Assert.Equal(boludito.Value.Valor, BoludometroCalculator.ValorDelDia(boludito.Value.UserId, 2026, 11, 2));
    }

    [Fact]
    public void BoluditoDelDia_DifferentOrder_ReturnsSameUser()
    {
        List<ulong> candidatos = [.. Enumerable.Range(1, 1000).Select(i => (ulong)i)];

        (ulong UserId, int Valor)? enOrden = BoludometroCalculator.BoluditoDelDia(candidatos, 2026, 11, 2);
        (ulong UserId, int Valor)? invertido = BoludometroCalculator.BoluditoDelDia(Enumerable.Reverse(candidatos), 2026, 11, 2);

        Assert.Equal(enOrden, invertido);
    }
}

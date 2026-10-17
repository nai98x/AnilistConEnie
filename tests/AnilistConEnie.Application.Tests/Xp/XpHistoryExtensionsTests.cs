using AnilistConEnie.Application.Xp;
using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpHistoryExtensionsTests
{
    private static UserDailyXp Day(int year, int month, int day, long xp) =>
        new() { Date = new DateTime(year, month, day), Xp = xp };

    [Fact]
    public void GetPromedio_EmptyList_ReturnsZero()
    {
        Assert.Equal(0, new List<UserDailyXp>().GetPromedio());
    }

    [Fact]
    public void GetPromedio_SingleRecord_ReturnsZero()
    {
        // Sin días transcurridos no hay promedio.
        Assert.Equal(0, new List<UserDailyXp> { Day(2026, 1, 1, 500) }.GetPromedio());
    }

    [Fact]
    public void GetPromedio_TwoRecords_ReturnsXpGainedDividedByDays()
    {
        List<UserDailyXp> registros =
        [
            Day(2026, 1, 1, 100),
            Day(2026, 1, 6, 600),
        ];

        // (600 - 100) / 5 días = 100 por día.
        Assert.Equal(100, registros.GetPromedio());
    }

    [Fact]
    public void GetPromedio_IntermediatePeak_UsesOnlyFirstAndLastRecord()
    {
        List<UserDailyXp> registros =
        [
            Day(2026, 1, 1, 0),
            Day(2026, 1, 2, 9999),
            Day(2026, 1, 11, 1000),
        ];

        // (1000 - 0) / 10 días = 100, ignorando el pico intermedio.
        Assert.Equal(100, registros.GetPromedio());
    }

    private static XpDiarioExtremos Extremos(DateTime primera, long primerXp, DateTime ultima, long ultimoXp) =>
        new() { PrimeraFecha = primera, PrimerXp = primerXp, UltimaFecha = ultima, UltimoXp = ultimoXp };

    [Fact]
    public void GetPromedio_ExtremosLastInCurrentYear_CountsUntilToday()
    {
        XpDiarioExtremos extremos = Extremos(new DateTime(2026, 1, 1), 0, new DateTime(2026, 1, 6), 1000);

        // (1000 - 0) / 10 días hasta hoy (11/1), no hasta el último registro.
        Assert.Equal(100, extremos.GetPromedio(new DateTime(2026, 1, 11, 18, 30, 0)));
    }

    [Fact]
    public void GetPromedio_ExtremosLastInPreviousYear_CountsUntilLastRecord()
    {
        XpDiarioExtremos extremos = Extremos(new DateTime(2025, 1, 1), 0, new DateTime(2025, 1, 11), 1000);

        Assert.Equal(100, extremos.GetPromedio(new DateTime(2026, 3, 1)));
    }

    [Fact]
    public void GetPromedio_ExtremosWithoutElapsedDays_ReturnsZero()
    {
        XpDiarioExtremos extremos = Extremos(new DateTime(2026, 3, 1), 500, new DateTime(2026, 3, 1), 500);

        Assert.Equal(0, extremos.GetPromedio(new DateTime(2026, 3, 1, 10, 0, 0)));
    }

    [Fact]
    public void GetPromedio_Extremos_MatchesFilledChartAverage()
    {
        DateTime hoy = new(2026, 1, 11, 12, 0, 0);
        List<UserDailyXp> historial = [Day(2026, 1, 1, 0), Day(2026, 1, 4, 300), Day(2026, 1, 6, 1000)];

        XpChartResult chart = XpChartBuilder.Build(1, historial, hoy, includeZeroXp: false, fillMissingDays: true);
        XpDiarioExtremos extremos = Extremos(new DateTime(2026, 1, 1), 0, new DateTime(2026, 1, 6), 1000);

        Assert.Equal(chart.Points.GetPromedio(), extremos.GetPromedio(hoy));
    }
}

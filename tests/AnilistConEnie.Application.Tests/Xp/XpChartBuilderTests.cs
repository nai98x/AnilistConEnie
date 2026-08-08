using AnilistConEnie.Application.Xp;
using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpChartBuilderTests
{
    private const long UserId = 7;

    private static UserDailyXp Day(int year, int month, int day, long xp) =>
        new() { UserId = UserId, Date = new DateTime(year, month, day), Xp = xp };

    [Fact]
    public void Build_NoFill_AddsTodayPointIfMissing()
    {
        DateTime today = new(2026, 6, 25);
        List<UserDailyXp> history = [Day(2026, 6, 20, 1000)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: false, fillMissingDays: false);

        Assert.Empty(result.MissingDaysToPersist);
        Assert.Equal(2, result.Points.Count);
        UserDailyXp last = result.Points[^1];
        Assert.Equal(today.Date, last.Date);
        Assert.Equal(1000, last.Xp); // arrastra la última xp conocida
    }

    [Fact]
    public void Build_NoFill_DoesNotDuplicateTodayPoint()
    {
        DateTime today = new(2026, 6, 25);
        List<UserDailyXp> history = [Day(2026, 6, 20, 1000), Day(2026, 6, 25, 1500)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: false, fillMissingDays: false);

        Assert.Equal(2, result.Points.Count);
        Assert.Equal(1500, result.Points[^1].Xp);
    }

    [Fact]
    public void Build_NoFill_KeepsPreviousYears()
    {
        DateTime today = new(2026, 6, 25);
        List<UserDailyXp> history = [Day(2025, 12, 31, 800), Day(2026, 6, 25, 1500)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: false, fillMissingDays: false);

        Assert.Equal(2, result.Points.Count);
        Assert.Equal(new DateTime(2025, 12, 31), result.Points[0].Date);
    }

    [Fact]
    public void Build_WithFill_FillsMissingDaysCarryingXp()
    {
        DateTime today = new(2026, 1, 5);
        List<UserDailyXp> history = [Day(2026, 1, 1, 100), Day(2026, 1, 3, 300)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: false, fillMissingDays: true);

        // Días 1 al 5: 1(100), 2(rellenado 100), 3(300), 4(rellenado 300), 5(rellenado 300).
        Assert.Equal(5, result.Points.Count);
        Assert.Equal([100, 100, 300, 300, 300], result.Points.Select(p => p.Xp));
    }

    [Fact]
    public void Build_WithFill_MarksOnlyFilledDaysWithXpForPersist()
    {
        DateTime today = new(2026, 1, 5);
        List<UserDailyXp> history = [Day(2026, 1, 1, 100), Day(2026, 1, 3, 300)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: false, fillMissingDays: true);

        // Solo los días generados (2, 4, 5) con xp != 0 se marcan para persistir; los reales no.
        Assert.Equal(3, result.MissingDaysToPersist.Count);
        Assert.All(result.MissingDaysToPersist, d => Assert.NotEqual(0, d.Xp));
        Assert.Equal(
            [new DateTime(2026, 1, 2), new DateTime(2026, 1, 4), new DateTime(2026, 1, 5)],
            result.MissingDaysToPersist.Select(d => d.Date).OrderBy(d => d));
    }

    [Fact]
    public void Build_WithFill_OmitsZeroDaysWhenZeroExcluded()
    {
        DateTime today = new(2026, 1, 4);
        // El primer registro es el día 3: los días 1 y 2 quedan con lastXp = 0.
        List<UserDailyXp> history = [Day(2026, 1, 3, 300)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: false, fillMissingDays: true);

        // Días 1 y 2 (xp 0) se omiten; quedan 3(300) y 4(rellenado 300).
        Assert.Equal([300, 300], result.Points.Select(p => p.Xp));
    }

    [Fact]
    public void Build_WithFill_IncludesZeroDaysWhenRequested()
    {
        DateTime today = new(2026, 1, 4);
        List<UserDailyXp> history = [Day(2026, 1, 3, 300)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: true, fillMissingDays: true);

        // Días 1(0), 2(0), 3(300), 4(300).
        Assert.Equal([0, 0, 300, 300], result.Points.Select(p => p.Xp));
        // Los días en cero no se persisten.
        Assert.All(result.MissingDaysToPersist, d => Assert.NotEqual(0, d.Xp));
    }

    [Fact]
    public void Build_WithFill_NoYearRecords_ProducesNoPoints()
    {
        DateTime today = new(2026, 6, 25);
        List<UserDailyXp> history = [Day(2025, 5, 1, 500)];

        XpChartResult result = XpChartBuilder.Build(UserId, history, today, includeZeroXp: true, fillMissingDays: true);

        // Solo el registro del año anterior; no se rellena nada del año actual.
        Assert.Single(result.Points);
        Assert.Empty(result.MissingDaysToPersist);
    }
}

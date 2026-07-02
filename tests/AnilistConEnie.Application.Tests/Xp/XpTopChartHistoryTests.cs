using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpTopChartHistoryTests
{
    private const long UserId = 7;

    private static UserDailyXp Day(DateTime date, long xp) => new() { UserId = UserId, Date = date, Xp = xp };

    [Fact]
    public void Build_NoHistory_ReturnsSingleTodayPointWithCurrentXp()
    {
        DateTime today = new(2026, 6, 25);

        List<UserDailyXp> points = XpTopChartHistory.Build(UserId, [], today, currentXp: 500);

        UserDailyXp point = Assert.Single(points);
        Assert.Equal(today, point.Date);
        Assert.Equal(500, point.Xp);
    }

    [Fact]
    public void Build_HistoryOlderThan365Days_WindowIsCappedAtOneYearAndStepsWeekly()
    {
        DateTime today = new(2026, 6, 25);
        List<UserDailyXp> history = [Day(today.AddYears(-3), 100), Day(today, 9000)];

        List<UserDailyXp> points = XpTopChartHistory.Build(UserId, history, today, currentXp: 9000);

        // 365 no es múltiplo de 7: el primer checkpoint cae dentro de la semana previa al límite
        // (nunca antes), y el resto son pasos exactos de 7 días terminando hoy.
        DateTime windowStart = today.AddDays(-365);
        Assert.InRange(points[0].Date, windowStart, windowStart.AddDays(6));
        Assert.Equal(today, points[^1].Date);
        Assert.All(points.Zip(points.Skip(1)), pair => Assert.Equal(7, (pair.Second.Date - pair.First.Date).Days));
    }

    [Fact]
    public void Build_UserNewerThan365Days_StartsAtFirstRecordInsteadOfPadding()
    {
        DateTime today = new(2026, 6, 25);
        DateTime joined = today.AddDays(-30);
        List<UserDailyXp> history = [Day(joined, 50), Day(today, 800)];

        List<UserDailyXp> points = XpTopChartHistory.Build(UserId, history, today, currentXp: 800);

        // No pasos de 7 días caen justo en la fecha de ingreso: el primer checkpoint arranca
        // después (nunca antes), dentro de la semana siguiente a que se unió.
        Assert.InRange(points[0].Date, joined, joined.AddDays(6));
        Assert.Equal(today, points[^1].Date);
    }

    [Fact]
    public void Build_CarriesLastKnownXpForwardBetweenCheckpoints()
    {
        DateTime today = new(2026, 1, 22); // 3 semanas después del ingreso
        DateTime joined = today.AddDays(-21);
        List<UserDailyXp> history = [Day(joined, 100), Day(joined.AddDays(10), 400)];

        List<UserDailyXp> points = XpTopChartHistory.Build(UserId, history, today, currentXp: 900);

        // Checkpoints: joined(100), +7d(100, todavía no llega el registro del día 10),
        // +14d(400, ya pasó el registro del día 10), today/+21d(900, total en vivo).
        Assert.Equal([100, 100, 400, 900], points.Select(p => p.Xp));
    }

    [Fact]
    public void Build_LastPointAlwaysUsesLiveCurrentXpRegardlessOfPersistedValue()
    {
        DateTime today = new(2026, 6, 25);
        List<UserDailyXp> history = [Day(today.AddDays(-10), 100), Day(today, 500)];

        List<UserDailyXp> points = XpTopChartHistory.Build(UserId, history, today, currentXp: 750);

        Assert.Equal(today, points[^1].Date);
        Assert.Equal(750, points[^1].Xp);
    }
}

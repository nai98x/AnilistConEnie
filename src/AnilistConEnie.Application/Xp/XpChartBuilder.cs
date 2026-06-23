using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Xp;

public sealed record XpChartResult(
    IReadOnlyList<UserDailyXp> Points,
    IReadOnlyList<UserDailyXp> MissingDaysToPersist);

public static class XpChartBuilder
{
    public static XpChartResult Build(
        long userId,
        IReadOnlyList<UserDailyXp> history,
        DateTime today,
        bool includeZeroXp,
        bool fillMissingDays)
    {
        int currentYear = today.Year;
        DateTime todayDate = today.Date;

        List<UserDailyXp> previousYears = history.Where(x => x.Date.Year != currentYear).OrderBy(x => x.Date).ToList();
        List<UserDailyXp> currentYearRecords = history.Where(x => x.Date.Year == currentYear).OrderBy(x => x.Date).ToList();

        List<UserDailyXp> points = [..previousYears];

        if (!fillMissingDays)
        {
            points.AddRange(currentYearRecords);

            if (currentYearRecords.All(x => x.Date.Date != todayDate))
            {
                long lastXp = currentYearRecords.LastOrDefault()?.Xp ?? previousYears.LastOrDefault()?.Xp ?? 0;
                points.Add(new UserDailyXp { Date = todayDate, UserId = userId, Xp = lastXp });
            }

            return new XpChartResult(points.OrderBy(x => x.Date).ToList(), []);
        }

        List<UserDailyXp> missingDays = [];

        if (currentYearRecords.Count > 0)
        {
            DateTime day = new(currentYear, 1, 1);
            long lastXp = 0;
            int idx = 0;

            while (day <= todayDate)
            {
                if (idx < currentYearRecords.Count && currentYearRecords[idx].Date.Date == day)
                {
                    points.Add(currentYearRecords[idx]);
                    lastXp = currentYearRecords[idx].Xp;
                    idx++;
                }
                else if (includeZeroXp || lastXp != 0)
                {
                    UserDailyXp filled = new() { Date = day, UserId = userId, Xp = lastXp };
                    points.Add(filled);

                    if (lastXp != 0)
                        missingDays.Add(filled);
                }

                day = day.AddDays(1);
            }
        }

        return new XpChartResult(points.OrderBy(x => x.Date).ToList(), missingDays);
    }
}

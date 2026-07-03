using AnilistConEnie.Model.Entities.Charts;

namespace AnilistConEnie.Application.Charts;

/// <summary>
/// Arma los <see cref="ChartSpec"/> de los gráficos de XP a partir de datos ya calculados. El
/// renderer (Infrastructure) decide cómo dibujar cada tipo de spec.
/// </summary>
public static class XpCharts
{
    public static BarProgressSpec ProgressBar(long total, long max) => new(total, max);

    public static DonutChartSpec Distribution(IReadOnlyList<PieSlice> slices) => new(slices, DonutFraction: 0);

    public static LineChartSpec History(IReadOnlyList<(long Xp, DateTime Date)> points, long min, long max) => new(
        Values: [.. points.Select(x => (double)x.Xp)],
        XLabels: [.. points.Select(x => x.Date.ToString("dd/MM/yyyy"))],
        Min: min,
        Max: max,
        Fill: true);

    public static MultiLineChartSpec TopMultiLine(IReadOnlyList<LineSeries> series, IReadOnlyList<DateTime> labelDates, long min, long max) => new(
        Series: series,
        XLabels: [.. labelDates.Select(d => $"{d.Day}/{d.Month}/{d.Year}")],
        Min: min,
        Max: max);

    public static DonutChartSpec CountryPie(IReadOnlyList<PieSlice> slices) => new(slices);
}

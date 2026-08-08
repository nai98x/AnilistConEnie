using AnilistConEnie.Application.Charts;
using AnilistConEnie.Domain.Entities.Charts;

namespace AnilistConEnie.Application.Tests.Charts;

public class XpChartsTests
{
    [Fact]
    public void ProgressBar_UsaTotalYMax()
    {
        BarProgressSpec spec = XpCharts.ProgressBar(300, 1000);
        Assert.Equal(300, spec.Value);
        Assert.Equal(1000, spec.Max);
    }

    [Fact]
    public void Distribution_DonutFractionCero()
    {
        IReadOnlyList<PieSlice> slices = [new("A", 1, "#111111")];
        DonutChartSpec spec = XpCharts.Distribution(slices);

        Assert.Equal(0, spec.DonutFraction);
        Assert.Same(slices, spec.Slices);
    }

    [Fact]
    public void History_MapeaXpYFormateaFechas()
    {
        List<(long Xp, DateTime Date)> puntos =
        [
            (100, new DateTime(2026, 1, 5)),
            (250, new DateTime(2026, 2, 10))
        ];

        LineChartSpec spec = XpCharts.History(puntos, 0, 300);

        Assert.Equal([100d, 250d], spec.Values);
        Assert.Equal(["05/01/2026", "10/02/2026"], spec.XLabels);
        Assert.Equal(0, spec.Min);
        Assert.Equal(300, spec.Max);
        Assert.True(spec.Fill);
    }

    [Fact]
    public void TopMultiLine_FormateaLabelsSinCeros()
    {
        IReadOnlyList<LineSeries> series = [new("User", [1d, 2d], "#abcdef")];
        IReadOnlyList<DateTime> fechas = [new DateTime(2026, 3, 4), new DateTime(2026, 11, 20)];

        MultiLineChartSpec spec = XpCharts.TopMultiLine(series, fechas, 0, 10);

        Assert.Same(series, spec.Series);
        Assert.Equal(["4/3/2026", "20/11/2026"], spec.XLabels);
    }

    [Fact]
    public void CountryPie_UsaDonutFractionPorDefecto()
    {
        IReadOnlyList<PieSlice> slices = [new("AR", 5, "#75AADB")];
        DonutChartSpec spec = XpCharts.CountryPie(slices);

        Assert.Equal(0.55, spec.DonutFraction);
        Assert.Same(slices, spec.Slices);
    }
}

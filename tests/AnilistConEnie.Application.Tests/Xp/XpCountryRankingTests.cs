using AnilistConEnie.Application.Xp;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpCountryRankingTests
{
    private const ulong OtrosId = 999;

    [Fact]
    public void BuildChartSeries_MoreCountriesThanSlices_GroupsRestIntoOtros()
    {
        // maxPorciones = 3: Argentina y Chile entran; México se suma a Otros.
        List<CountryXp> perCountry =
        [
            new(1, "Argentina", 500),
            new(2, "Chile", 300),
            new(3, "México", 100),
            new(OtrosId, "Otros", 50),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, maxPorciones: 3);

        Assert.Equal(["Argentina", "Chile", "Otros"], series.Select(x => x.Name));
        Assert.Equal(150, series[2].Xp);
    }

    [Fact]
    public void BuildChartSeries_AnyInput_PreservesTotalXp()
    {
        List<CountryXp> perCountry =
        [
            new(1, "A", 500),
            new(2, "B", 300),
            new(3, "C", 200),
            new(4, "D", 100),
            new(OtrosId, "Otros", 50),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, maxPorciones: 3);

        Assert.Equal(3, series.Count);
        Assert.Equal(perCountry.Sum(x => x.Xp), series.Sum(x => x.Xp));
    }

    [Fact]
    public void BuildChartSeries_AllCountriesFit_DoesNotGroupAndOmitsEmptyOtros()
    {
        List<CountryXp> perCountry =
        [
            new(1, "A", 100),
            new(2, "B", 400),
            new(3, "C", 200),
            new(OtrosId, "Otros", 0),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, maxPorciones: 6);

        Assert.Equal(["B", "C", "A"], series.Select(x => x.Name));
    }

    [Fact]
    public void BuildChartSeries_LargeOtros_CanLead()
    {
        List<CountryXp> perCountry =
        [
            new(1, "A", 100),
            new(2, "B", 90),
            new(3, "C", 80),
            new(OtrosId, "Otros", 1000),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, maxPorciones: 3);

        // A y B entran; C (80) se suma a Otros (1000), que queda primero.
        Assert.Equal(["Otros", "A", "B"], series.Select(x => x.Name));
        Assert.Equal(1080, series[0].Xp);
    }

    [Fact]
    public void BuildChartSeries_NoOtrosCountry_GroupsRemainderIntoOtros()
    {
        List<CountryXp> perCountry =
        [
            new(1, "Argentina", 500),
            new(2, "Chile", 300),
            new(3, "México", 100),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, maxPorciones: 2);

        Assert.Equal(["Argentina", "Otros"], series.Select(x => x.Name));
        Assert.Equal(400, series[1].Xp);
    }

    [Fact]
    public void BuildChartSeries_NoCountries_ReturnsEmpty()
    {
        Assert.Empty(XpCountryRanking.BuildChartSeries([], OtrosId, maxPorciones: 6));
    }
}

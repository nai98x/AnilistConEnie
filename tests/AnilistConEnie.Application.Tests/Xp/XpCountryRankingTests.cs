using AnilistConEnie.Application.Xp;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpCountryRankingTests
{
    private const ulong OtrosId = 999;

    [Fact]
    public void BuildChartSeries_agrupa_los_que_quedan_fuera_del_top_en_Otros()
    {
        // topN = 2: Argentina y Chile entran al top; México queda fuera y se suma a Otros.
        List<CountryXp> perCountry =
        [
            new(1, "Argentina", 500),
            new(2, "Chile", 300),
            new(3, "México", 100),
            new(OtrosId, "Otros", 50),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, topN: 2);

        // top (Arg 500, Chile 300) + Otros (100 + 50 = 150). Se descarta el último (Otros).
        Assert.Equal(2, series.Count);
        Assert.Equal("Argentina", series[0].Name);
        Assert.Equal(500, series[0].Xp);
        Assert.Equal("Chile", series[1].Name);
        Assert.Equal(300, series[1].Xp);
    }

    [Fact]
    public void BuildChartSeries_ordena_descendente_y_descarta_el_ultimo()
    {
        List<CountryXp> perCountry =
        [
            new(1, "A", 100),
            new(2, "B", 400),
            new(3, "C", 200),
            new(OtrosId, "Otros", 0),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, topN: 10);

        // Otros queda en 0 y, al ordenar desc, es el último -> se descarta. Quedan B, C, A.
        Assert.Equal(["B", "C", "A"], series.Select(x => x.Name));
    }

    [Fact]
    public void BuildChartSeries_Otros_puede_quedar_arriba_si_acumula_mas()
    {
        List<CountryXp> perCountry =
        [
            new(1, "A", 100),
            new(2, "B", 90),
            new(3, "C", 80),
            new(OtrosId, "Otros", 1000),
        ];

        IReadOnlyList<CountryXp> series = XpCountryRanking.BuildChartSeries(perCountry, OtrosId, topN: 2);

        // Otros (1000 + 80 sobrante) lidera; se descarta el último (C con 80... ya está en Otros) -> queda Otros, A.
        Assert.Equal("Otros", series[0].Name);
        Assert.Equal(1080, series[0].Xp);
    }
}

namespace AnilistConEnie.Application.Xp;

/// <summary>XP acumulada por un país del servidor.</summary>
public readonly record struct CountryXp(ulong CountryId, string Name, long Xp);

public static class XpCountryRanking
{
    /// <summary>
    /// Arma la serie para el gráfico de XP por país: los <paramref name="topN"/> países con más XP, más una
    /// entrada "Otros" que agrupa al país marcado como otros (<paramref name="otrosCountryId"/>) y a todos los
    /// que quedaron fuera del top. La serie se devuelve ordenada de mayor a menor descartando el último puesto.
    /// </summary>
    public static IReadOnlyList<CountryXp> BuildChartSeries(IReadOnlyList<CountryXp> perCountry, ulong otrosCountryId, int topN = 10)
    {
        CountryXp otros = perCountry.First(x => x.CountryId == otrosCountryId);
        List<CountryXp> sinOtros = perCountry
            .Where(x => x.CountryId != otrosCountryId)
            .OrderByDescending(x => x.Xp)
            .ToList();

        List<CountryXp> top = sinOtros.Take(topN).ToList();
        long otrosXp = sinOtros.Skip(topN).Sum(x => x.Xp) + otros.Xp;

        List<CountryXp> folded = [.. top, otros with { Xp = otrosXp }];
        folded = folded.OrderByDescending(x => x.Xp).ToList();
        folded.RemoveAt(folded.Count - 1);
        return folded;
    }
}

namespace AnilistConEnie.Application.Xp;

/// <summary>XP acumulada por un país del servidor.</summary>
public readonly record struct CountryXp(ulong CountryId, string Name, long Xp);

public static class XpCountryRanking
{
    /// <summary>
    /// Arma la serie para el gráfico de XP por país con a lo sumo <paramref name="maxPorciones"/> entradas: los
    /// países con más XP y una entrada "Otros" que agrupa al país marcado como otros (<paramref name="otrosCountryId"/>)
    /// y a todos los que no entran. No se descarta XP: la serie suma lo mismo que <paramref name="perCountry"/>.
    /// "Otros" se omite si queda en cero. La serie se devuelve ordenada de mayor a menor.
    /// </summary>
    public static IReadOnlyList<CountryXp> BuildChartSeries(IReadOnlyList<CountryXp> perCountry, ulong otrosCountryId, int maxPorciones)
    {
        CountryXp otros = perCountry.FirstOrDefault(x => x.CountryId == otrosCountryId, new CountryXp(otrosCountryId, "Otros", 0));
        List<CountryXp> sinOtros = perCountry
            .Where(x => x.CountryId != otrosCountryId)
            .OrderByDescending(x => x.Xp)
            .ToList();

        int lugaresParaPaises = sinOtros.Count + (otros.Xp > 0 ? 1 : 0) <= maxPorciones ? sinOtros.Count : maxPorciones - 1;

        List<CountryXp> top = sinOtros.Take(lugaresParaPaises).ToList();
        long otrosXp = sinOtros.Skip(lugaresParaPaises).Sum(x => x.Xp) + otros.Xp;

        List<CountryXp> serie = otrosXp > 0 ? [.. top, otros with { Xp = otrosXp }] : top;
        return serie.OrderByDescending(x => x.Xp).ToList();
    }
}

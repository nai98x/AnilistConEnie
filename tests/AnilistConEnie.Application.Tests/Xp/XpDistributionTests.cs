using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpDistributionTests
{
    [Fact]
    public void Build_reparte_las_cinco_categorias()
    {
        UserXp rank = new()
        {
            Total = 1000,
            Challenges = 100,
            Eventos = 200,
            Intercambios = 50,
            Otros = 150,
        };

        IReadOnlyList<XpCategoryShare> shares = XpDistribution.Build(rank);

        Assert.Equal(5, shares.Count);
        // mensajes = 1000 - 100 - 200 - 50 - 150 = 500
        Assert.Equal(500, shares.Single(s => s.Category == XpCategory.Mensajes).Value);
        Assert.Equal(100, shares.Single(s => s.Category == XpCategory.Challenges).Value);
        Assert.Equal(200, shares.Single(s => s.Category == XpCategory.Eventos).Value);
        Assert.Equal(50, shares.Single(s => s.Category == XpCategory.Intercambios).Value);
        Assert.Equal(150, shares.Single(s => s.Category == XpCategory.Otros).Value);
    }

    [Fact]
    public void Build_los_porcentajes_suman_cien()
    {
        UserXp rank = new()
        {
            Total = 1000,
            Challenges = 100,
            Eventos = 200,
            Intercambios = 50,
            Otros = 150,
        };

        IReadOnlyList<XpCategoryShare> shares = XpDistribution.Build(rank);

        Assert.Equal(100m, shares.Sum(s => s.Percentage));
    }

    [Fact]
    public void Build_porcentaje_de_mensajes_absorbe_el_redondeo()
    {
        // Valores que no dividen exacto para forzar el ajuste en Mensajes.
        UserXp rank = new()
        {
            Total = 3,
            Challenges = 1,
            Eventos = 1,
            Intercambios = 0,
            Otros = 0,
        };

        IReadOnlyList<XpCategoryShare> shares = XpDistribution.Build(rank);

        // 1/3 truncado a entero = 33% cada una; mensajes = 100 - 33 - 33 = 34.
        Assert.Equal(33m, shares.Single(s => s.Category == XpCategory.Challenges).Percentage);
        Assert.Equal(34m, shares.Single(s => s.Category == XpCategory.Mensajes).Percentage);
        Assert.Equal(100m, shares.Sum(s => s.Percentage));
    }
}

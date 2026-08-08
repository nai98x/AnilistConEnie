using AnilistConEnie.Application.Premios;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Application.Tests.Premios;

public class PremioFactoryTests
{
    [Fact]
    public void Crear_SeasonAndYear_BuildsNombreAndOrder()
    {
        Premio premio = PremioFactory.Crear(2026, Season.Winter, "https://link");

        Assert.Equal("Winter 2026", premio.Nombre);
        Assert.Equal("https://link", premio.Link);
        Assert.Equal(2026, premio.Year);
        Assert.Equal((int)Season.Winter, premio.Order);
    }

    [Theory]
    [InlineData(Season.Winter, "Winter 2025")]
    [InlineData(Season.Spring, "Spring 2025")]
    [InlineData(Season.Summer, "Summer 2025")]
    [InlineData(Season.Fall, "Fall 2025")]
    [InlineData(Season.Anual, "Anual 2025")]
    public void Crear_EachSeason_UsesSeasonName(Season season, string nombreEsperado)
    {
        Premio premio = PremioFactory.Crear(2025, season, "url");

        Assert.Equal(nombreEsperado, premio.Nombre);
        Assert.Equal((int)season, premio.Order);
    }
}

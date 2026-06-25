using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpHistoryExtensionsTests
{
    private static UserDailyXp Day(int year, int month, int day, long xp) =>
        new() { Date = new DateTime(year, month, day), Xp = xp };

    [Fact]
    public void GetPromedio_lista_vacia_devuelve_cero()
    {
        Assert.Equal(0, new List<UserDailyXp>().GetPromedio());
    }

    [Fact]
    public void GetPromedio_un_solo_registro_devuelve_cero()
    {
        // Sin días transcurridos no hay promedio.
        Assert.Equal(0, new List<UserDailyXp> { Day(2026, 1, 1, 500) }.GetPromedio());
    }

    [Fact]
    public void GetPromedio_es_la_xp_ganada_dividida_por_los_dias()
    {
        List<UserDailyXp> registros =
        [
            Day(2026, 1, 1, 100),
            Day(2026, 1, 6, 600),
        ];

        // (600 - 100) / 5 días = 100 por día.
        Assert.Equal(100, registros.GetPromedio());
    }

    [Fact]
    public void GetPromedio_usa_solo_el_primer_y_ultimo_registro()
    {
        List<UserDailyXp> registros =
        [
            Day(2026, 1, 1, 0),
            Day(2026, 1, 2, 9999),
            Day(2026, 1, 11, 1000),
        ];

        // (1000 - 0) / 10 días = 100, ignorando el pico intermedio.
        Assert.Equal(100, registros.GetPromedio());
    }
}

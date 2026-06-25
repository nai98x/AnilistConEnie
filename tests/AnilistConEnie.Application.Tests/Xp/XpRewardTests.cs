using AnilistConEnie.Application.Xp;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpRewardTests
{
    /// <summary>Random con semilla fija para que el resultado sea determinista en los tests.</summary>
    private static Random Seeded() => new(12345);

    [Fact]
    public void Accrue_sin_booster_no_otorga_xp_de_booster()
    {
        UserXp current = new() { Total = 1000, Booster = 50 };

        XpAccrual result = XpReward.Accrue(current, isBooster: false, Seeded(), 10, 20, 5, 15);

        Assert.Equal(0, result.BoosterXp);
        Assert.InRange(result.BaseXp, 10, 20);
        Assert.Equal(result.BaseXp, result.TotalGranted);
        Assert.Equal(50, result.NewBoosterTotal);
        Assert.Equal(1000 + result.TotalGranted, result.NewGrandTotal);
    }

    [Fact]
    public void Accrue_con_booster_suma_base_mas_extra()
    {
        UserXp current = new() { Total = 1000, Booster = 50 };

        XpAccrual result = XpReward.Accrue(current, isBooster: true, Seeded(), 10, 20, 5, 15);

        Assert.InRange(result.BaseXp, 10, 20);
        Assert.InRange(result.BoosterXp, 5, 15);
        Assert.Equal(result.BaseXp + result.BoosterXp, result.TotalGranted);
        Assert.Equal(50 + result.BoosterXp, result.NewBoosterTotal);
        Assert.Equal(1000 + result.TotalGranted, result.NewGrandTotal);
    }

    [Fact]
    public void Accrue_respeta_los_limites_inclusivos()
    {
        // min == max fuerza un valor exacto.
        UserXp current = new() { Total = 0, Booster = 0 };

        XpAccrual result = XpReward.Accrue(current, isBooster: true, Seeded(), 7, 7, 3, 3);

        Assert.Equal(7, result.BaseXp);
        Assert.Equal(3, result.BoosterXp);
        Assert.Equal(10, result.TotalGranted);
        Assert.Equal(3, result.NewBoosterTotal);
        Assert.Equal(10, result.NewGrandTotal);
    }

    [Fact]
    public void Accrue_es_determinista_con_la_misma_semilla()
    {
        UserXp current = new() { Total = 500, Booster = 0 };

        XpAccrual a = XpReward.Accrue(current, isBooster: true, Seeded(), 1, 100, 1, 100);
        XpAccrual b = XpReward.Accrue(current, isBooster: true, Seeded(), 1, 100, 1, 100);

        Assert.Equal(a, b);
    }
}

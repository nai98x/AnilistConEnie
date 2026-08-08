using AnilistConEnie.Application.Xp;

namespace AnilistConEnie.Application.Tests.Xp;

public class XpRewardTests
{
    /// <summary>Random con semilla fija para que el resultado sea determinista en los tests.</summary>
    private static Random Seeded() => new(12345);

    [Fact]
    public void Accrue_WithoutBooster_GrantsNoBoosterXp()
    {
        XpAccrual result = XpReward.Accrue(isBooster: false, Seeded(), 10, 20, 5, 15);

        Assert.Equal(0, result.BoosterXp);
        Assert.InRange(result.BaseXp, 10, 20);
        Assert.Equal(result.BaseXp, result.TotalGranted);
    }

    [Fact]
    public void Accrue_WithBooster_AddsBasePlusExtra()
    {
        XpAccrual result = XpReward.Accrue(isBooster: true, Seeded(), 10, 20, 5, 15);

        Assert.InRange(result.BaseXp, 10, 20);
        Assert.InRange(result.BoosterXp, 5, 15);
        Assert.Equal(result.BaseXp + result.BoosterXp, result.TotalGranted);
    }

    [Fact]
    public void Accrue_RespectsInclusiveBounds()
    {
        // min == max fuerza un valor exacto.
        XpAccrual result = XpReward.Accrue(isBooster: true, Seeded(), 7, 7, 3, 3);

        Assert.Equal(7, result.BaseXp);
        Assert.Equal(3, result.BoosterXp);
        Assert.Equal(10, result.TotalGranted);
    }

    [Fact]
    public void Accrue_SameSeed_IsDeterministic()
    {
        XpAccrual a = XpReward.Accrue(isBooster: true, Seeded(), 1, 100, 1, 100);
        XpAccrual b = XpReward.Accrue(isBooster: true, Seeded(), 1, 100, 1, 100);

        Assert.Equal(a, b);
    }
}

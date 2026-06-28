using AnilistConEnie.Application.Challenges;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Challenges;

public class ChallengePolicyTests
{
    private static readonly DateTime Hoy = new(2026, 6, 25);

    [Fact]
    public void EstaVencido_DueToday_ReturnsTrue()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = Hoy };

        Assert.True(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void EstaVencido_DuePast_ReturnsTrue()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = Hoy.AddDays(-3) };

        Assert.True(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void EstaVencido_DueFuture_ReturnsFalse()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = Hoy.AddDays(5) };

        Assert.False(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void EstaVencido_NotAvailable_ReturnsFalse()
    {
        Challenge challenge = new() { Disponible = false, Vencimiento = Hoy.AddDays(-3) };

        Assert.False(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void EstaVencido_NoDueDate_ReturnsFalse()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = null };

        Assert.False(ChallengePolicy.EstaVencido(challenge, Hoy));
    }
}

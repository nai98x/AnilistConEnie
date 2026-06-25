using AnilistConEnie.Application.Challenges;
using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Tests.Challenges;

public class ChallengePolicyTests
{
    private static readonly DateTime Hoy = new(2026, 6, 25);

    [Fact]
    public void EstaVencido_si_el_vencimiento_es_hoy()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = Hoy };

        Assert.True(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void EstaVencido_si_el_vencimiento_ya_paso()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = Hoy.AddDays(-3) };

        Assert.True(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void No_esta_vencido_si_el_vencimiento_es_futuro()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = Hoy.AddDays(5) };

        Assert.False(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void No_esta_vencido_si_no_esta_disponible()
    {
        Challenge challenge = new() { Disponible = false, Vencimiento = Hoy.AddDays(-3) };

        Assert.False(ChallengePolicy.EstaVencido(challenge, Hoy));
    }

    [Fact]
    public void No_esta_vencido_sin_fecha_de_vencimiento()
    {
        Challenge challenge = new() { Disponible = true, Vencimiento = null };

        Assert.False(ChallengePolicy.EstaVencido(challenge, Hoy));
    }
}

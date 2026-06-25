using AnilistConEnie.Application.Confessions;

namespace AnilistConEnie.Application.Tests.Confessions;

public class ConfessionRevealPolicyTests
{
    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(1, 5, 5)]
    [InlineData(10, 5, 50)]
    [InlineData(20, 5, 100)]
    public void RevealChancePercent_es_reacciones_por_porcentaje(int reacciones, int porcentaje, int esperado)
    {
        Assert.Equal(esperado, ConfessionRevealPolicy.RevealChancePercent(reacciones, porcentaje));
    }
}

namespace AnilistConEnie.Application.Confessions;

public static class ConfessionRevealPolicy
{
    public static int RevealChancePercent(int reactionCount, int porcentajePorReaccion) => reactionCount * porcentajePorReaccion;
}

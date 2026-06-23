namespace AnilistConEnie.Application.Confessions;

public static class ConfessionRevealPolicy
{
    private const int PercentPerReaction = 5;

    public static int RevealChancePercent(int reactionCount) => reactionCount * PercentPerReaction;
}

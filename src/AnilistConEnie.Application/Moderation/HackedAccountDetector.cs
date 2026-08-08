using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Moderation;

public static class HackedAccountDetector
{
    public static bool IsHacked(IReadOnlyList<BasicMessage> recentMessages, int canalesDistintos, double ventanaMinutos)
    {
        if (recentMessages.Count == 0) return false;

        bool sameContent = recentMessages.Select(m => m.Content).Distinct().Count() == 1;
        bool spreadAcrossChannels = recentMessages.Select(m => m.ChannelId).Distinct().Count() == canalesDistintos;
        if (!sameContent || !spreadAcrossChannels) return false;

        DateTime first = recentMessages[0].CreatedAt;
        foreach (BasicMessage message in recentMessages.Skip(1))
        {
            if (message.CreatedAt.Subtract(first).TotalMinutes >= ventanaMinutos) return false;
        }

        return true;
    }
}

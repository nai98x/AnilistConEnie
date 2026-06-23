using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Moderation;

public static class HackedAccountDetector
{
    private const int DistinctChannelsThreshold = 3;
    private const double MaxSpanMinutes = 1;

    public static bool IsHacked(IReadOnlyList<BasicMessage> recentMessages)
    {
        if (recentMessages.Count == 0) return false;

        bool sameContent = recentMessages.Select(m => m.Content).Distinct().Count() == 1;
        bool spreadAcrossChannels = recentMessages.Select(m => m.ChannelId).Distinct().Count() == DistinctChannelsThreshold;
        if (!sameContent || !spreadAcrossChannels) return false;

        DateTime first = recentMessages[0].CreatedAt;
        foreach (BasicMessage message in recentMessages.Skip(1))
        {
            if (message.CreatedAt.Subtract(first).TotalMinutes >= MaxSpanMinutes) return false;
        }

        return true;
    }
}

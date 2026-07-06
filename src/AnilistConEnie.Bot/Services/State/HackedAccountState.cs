using AnilistConEnie.Application.Moderation;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class HackedAccountState(AntiSpamSettings settings)
{
    private readonly ConcurrentDictionary<ulong, List<BasicMessage>> _lastMessagesUsers = new();

    public bool IsHackedAccount(ulong userId) =>
        _lastMessagesUsers.TryGetValue(userId, out var userMessages) && HackedAccountDetector.IsHacked(userMessages, settings.CanalesDistintos, settings.VentanaMinutos);

    public void AddRecentUserMessage(ulong userId, ulong channelId, string content)
    {
        _lastMessagesUsers.AddOrUpdate(
            userId,
            _ => [new(content, channelId, DateTime.UtcNow)],
            (_, existing) =>
            {
                var source = existing.Count == settings.CanalesDistintos ? existing.Skip(1) : existing.AsEnumerable();
                return [..source, new(content, channelId, DateTime.UtcNow)];
            });
    }
}

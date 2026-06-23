using AnilistConEnie.Application.Moderation;
using AnilistConEnie.Model.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class HackedAccountState
{
    private readonly ConcurrentDictionary<ulong, List<BasicMessage>> _lastMessagesUsers = new();

    public bool IsHackedAccount(ulong userId) =>
        _lastMessagesUsers.TryGetValue(userId, out var userMessages) && HackedAccountDetector.IsHacked(userMessages);

    public void AddRecentUserMessage(ulong userId, ulong channelId, string content)
    {
        _lastMessagesUsers.AddOrUpdate(
            userId,
            _ => [new(content, channelId, DateTime.Now)],
            (_, existing) =>
            {
                var source = existing.Count == 3 ? existing.Skip(1) : existing.AsEnumerable();
                return [..source, new(content, channelId, DateTime.Now)];
            });
    }
}

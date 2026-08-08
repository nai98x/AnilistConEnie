using AnilistConEnie.Application.Moderation;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Domain.Entities;
using DSharpPlus.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class HackedAccountState(AntiSpamSettings settings)
{
    private readonly ConcurrentDictionary<ulong, List<DiscordMessage>> _lastMessagesUsers = new();

    public bool IsHackedAccount(ulong userId) =>
        _lastMessagesUsers.TryGetValue(userId, out var userMessages) && HackedAccountDetector.IsHacked(ToBasic(userMessages), settings.CanalesDistintos, settings.VentanaMinutos);

    public void AddRecentUserMessage(ulong userId, DiscordMessage message)
    {
        _lastMessagesUsers.AddOrUpdate(
            userId,
            _ => [message],
            (_, existing) =>
            {
                var source = existing.Count == settings.CanalesDistintos ? existing.Skip(1) : existing.AsEnumerable();
                return [..source, message];
            });
    }

    public IReadOnlyList<DiscordMessage> TakeRecentUserMessages(ulong userId) =>
        _lastMessagesUsers.TryRemove(userId, out var mensajes) ? mensajes : [];

    // El diccionario suma una entrada por cada usuario que escribe; sin esto crecería sin cota. Una
    // entrada cuyo último mensaje quedó fuera de la ventana ya no puede disparar la detección.
    public void PruneStale()
    {
        DateTime corte = DateTime.UtcNow.AddMinutes(-settings.VentanaMinutos);
        foreach (var (userId, mensajes) in _lastMessagesUsers)
        {
            if (mensajes.Count == 0 || mensajes[^1].CreationTimestamp.UtcDateTime < corte)
                _lastMessagesUsers.TryRemove(userId, out _);
        }
    }

    private static List<BasicMessage> ToBasic(List<DiscordMessage> mensajes) =>
        [..mensajes.Select(x => new BasicMessage(x.Content, x.ChannelId, x.CreationTimestamp.UtcDateTime))];
}

using AnilistConEnie.Bot.Events.Handlers;
using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Events;

public static class EventHandlerRegistrar
{
    public static IServiceCollection AddDiscordEventHandlers(this IServiceCollection services)
    {
        services.AddSingleton<GuildDownloadCompletedHandler>();
        services.AddSingleton<MessageCreatedHandler>();
        services.AddSingleton<MessageUpdatedHandler>();
        services.AddSingleton<MessageDeletedHandler>();
        services.AddSingleton<MessageReactionAddedHandler>();
        services.AddSingleton<ComponentInteractionHandler>();
        services.AddSingleton<GuildMemberAddedHandler>();
        services.AddSingleton<GuildMemberRemovedHandler>();
        services.AddSingleton<SessionCreatedHandler>();
        services.AddSingleton<SessionResumedHandler>();
        services.AddSingleton<ZombiedHandler>();
        return services;
    }

    public static DiscordClientBuilder BindEventHandlers(this DiscordClientBuilder builder, IServiceProvider provider)
    {
        // La resolución de cada handler se difiere al momento en que se dispara el evento
        // (no al construir el DiscordClient). Esto evita el ciclo
        // DiscordClient -> Handler -> DiscordBotService -> DiscordClient,
        // permitiendo que los handlers inyecten DiscordBotService/BotStateService por constructor.
        builder.ConfigureEventHandlers(b => b
            .HandleGuildDownloadCompleted((c, e) => provider.GetRequiredService<GuildDownloadCompletedHandler>().Handle(c, e))
            .HandleMessageCreated((c, e) => provider.GetRequiredService<MessageCreatedHandler>().Handle(c, e))
            .HandleMessageUpdated((c, e) => provider.GetRequiredService<MessageUpdatedHandler>().Handle(c, e))
            .HandleMessageDeleted((c, e) => provider.GetRequiredService<MessageDeletedHandler>().Handle(c, e))
            .HandleMessageReactionAdded((c, e) => provider.GetRequiredService<MessageReactionAddedHandler>().Handle(c, e))
            .HandleComponentInteractionCreated((c, e) => provider.GetRequiredService<ComponentInteractionHandler>().Handle(c, e))
            .HandleGuildMemberAdded((c, e) => provider.GetRequiredService<GuildMemberAddedHandler>().Handle(c, e))
            .HandleGuildMemberRemoved((c, e) => provider.GetRequiredService<GuildMemberRemovedHandler>().Handle(c, e))
            .HandleSessionCreated((c, e) => provider.GetRequiredService<SessionCreatedHandler>().Handle(c, e))
            .HandleSessionResumed((c, e) => provider.GetRequiredService<SessionResumedHandler>().Handle(c, e))
            .HandleZombied((c, e) => provider.GetRequiredService<ZombiedHandler>().Handle(c, e))
        );
        return builder;
    }
}

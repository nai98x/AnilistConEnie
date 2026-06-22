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
        builder.ConfigureEventHandlers(b => b
            .HandleGuildDownloadCompleted(provider.GetRequiredService<GuildDownloadCompletedHandler>().Handle)
            .HandleMessageCreated(provider.GetRequiredService<MessageCreatedHandler>().Handle)
            .HandleMessageUpdated(provider.GetRequiredService<MessageUpdatedHandler>().Handle)
            .HandleMessageDeleted(provider.GetRequiredService<MessageDeletedHandler>().Handle)
            .HandleMessageReactionAdded(provider.GetRequiredService<MessageReactionAddedHandler>().Handle)
            .HandleComponentInteractionCreated(provider.GetRequiredService<ComponentInteractionHandler>().Handle)
            .HandleGuildMemberAdded(provider.GetRequiredService<GuildMemberAddedHandler>().Handle)
            .HandleGuildMemberRemoved(provider.GetRequiredService<GuildMemberRemovedHandler>().Handle)
            .HandleSessionCreated(provider.GetRequiredService<SessionCreatedHandler>().Handle)
            .HandleSessionResumed(provider.GetRequiredService<SessionResumedHandler>().Handle)
            .HandleZombied(provider.GetRequiredService<ZombiedHandler>().Handle)
        );
        return builder;
    }
}

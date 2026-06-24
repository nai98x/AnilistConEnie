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

    // La resolución de cada handler se difiere al momento en que se dispara el evento, vía
    // c.ServiceProvider (el contenedor del Host, que comparte el DiscordClient). Esto evita el ciclo
    // DiscordClient -> Handler -> DiscordBotService -> DiscordClient en el arranque,
    // permitiendo que los handlers inyecten DiscordBotService y los servicios de estado por constructor.
    public static EventHandlingBuilder BindEventHandlers(this EventHandlingBuilder builder)
    {
        return builder
            .HandleGuildDownloadCompleted((c, e) => c.ServiceProvider.GetRequiredService<GuildDownloadCompletedHandler>().Handle(c, e))
            .HandleMessageCreated((c, e) => c.ServiceProvider.GetRequiredService<MessageCreatedHandler>().Handle(c, e))
            .HandleMessageUpdated((c, e) => c.ServiceProvider.GetRequiredService<MessageUpdatedHandler>().Handle(c, e))
            .HandleMessageDeleted((c, e) => c.ServiceProvider.GetRequiredService<MessageDeletedHandler>().Handle(c, e))
            .HandleMessageReactionAdded((c, e) => c.ServiceProvider.GetRequiredService<MessageReactionAddedHandler>().Handle(c, e))
            .HandleComponentInteractionCreated((c, e) => c.ServiceProvider.GetRequiredService<ComponentInteractionHandler>().Handle(c, e))
            .HandleGuildMemberAdded((c, e) => c.ServiceProvider.GetRequiredService<GuildMemberAddedHandler>().Handle(c, e))
            .HandleGuildMemberRemoved((c, e) => c.ServiceProvider.GetRequiredService<GuildMemberRemovedHandler>().Handle(c, e))
            .HandleSessionCreated((c, e) => c.ServiceProvider.GetRequiredService<SessionCreatedHandler>().Handle(c, e))
            .HandleSessionResumed((c, e) => c.ServiceProvider.GetRequiredService<SessionResumedHandler>().Handle(c, e))
            .HandleZombied((c, e) => c.ServiceProvider.GetRequiredService<ZombiedHandler>().Handle(c, e));
    }
}

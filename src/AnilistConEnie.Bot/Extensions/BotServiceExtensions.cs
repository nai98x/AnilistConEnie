using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Extensions;

public static class BotServiceExtensions
{
    public static IServiceCollection AddBotServices(this IServiceCollection services)
    {
        // Estado en memoria del bot, separado por responsabilidad
        services.AddSingleton<EmoteModeState>();
        services.AddSingleton<AnilistUsersState>();
        services.AddSingleton<HighlightsState>();
        services.AddSingleton<TriggersState>();
        services.AddSingleton<MemberActivityState>();
        services.AddSingleton<InviteLinkState>();
        services.AddSingleton<HackedAccountState>();
        services.AddSingleton<TeiouCooldownState>();
        services.AddSingleton<XpState>();
        services.AddSingleton<BoluditosState>();
        services.AddSingleton<ConfessionsState>();
        services.AddSingleton<PermanentUsernameState>();

        // Helpers
        services.AddSingleton<DiscordHelper>();
        services.AddSingleton<AnilistHelper>();
        services.AddSingleton<BehaviorHelper>();

        // Servicio principal (hosted service)
        services.AddSingleton<DiscordBotService>();
        services.AddHostedService(sp => sp.GetRequiredService<DiscordBotService>());

        return services;
    }
}

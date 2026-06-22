using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Extensions;

public static class BotServiceExtensions
{
    public static IServiceCollection AddBotServices(this IServiceCollection services)
    {
        // Estado en memoria del bot
        services.AddSingleton<BotStateService>();

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

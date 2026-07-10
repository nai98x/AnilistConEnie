using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.Scheduling.Tasks;
using AnilistConEnie.Bot.Services.State;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Extensions;

public static class BotServiceExtensions
{
    public static IServiceCollection AddBotServices(this IServiceCollection services)
    {
        // Estado en memoria del bot, separado por responsabilidad
        services.AddSingleton<EmoteModeState>();
        services.AddSingleton<TriggersState>();
        services.AddSingleton<MemberActivityState>();
        services.AddSingleton<InviteLinkState>();
        services.AddSingleton<HackedAccountState>();
        services.AddSingleton<XpState>();
        services.AddSingleton<BoluditosState>();
        services.AddSingleton<ConfessionsState>();
        services.AddSingleton<PermanentUsernameState>();
        services.AddSingleton<ChallengePostsState>();
        services.AddSingleton<SubirImagenState>();

        // Helpers
        services.AddSingleton<RangoRoles>();
        services.AddSingleton<RelojPais>();
        services.AddSingleton<DiscordLogService>();
        services.AddSingleton<AnilistService>();
        services.AddSingleton<GuildMaintenanceService>();
        services.AddSingleton<FunService>();

        // HttpClient para descargas de imágenes y APIs externas (horóscopo, traducción)
        services.AddHttpClient();

        // Servicio principal (hosted service)
        services.AddSingleton<DiscordBotService>();
        services.AddHostedService(sp => sp.GetRequiredService<DiscordBotService>());

        // Tareas programadas (corren en paralelo al procesamiento de comandos)
        services.AddHostedService<MinuteScheduledService>();
        services.AddHostedService<HourlyScheduledService>();
        services.AddHostedService<DailyScheduledService>();

        return services;
    }
}

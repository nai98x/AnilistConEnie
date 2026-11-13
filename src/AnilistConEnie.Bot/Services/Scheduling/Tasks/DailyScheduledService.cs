using AnilistConEnie.Application.Actividad;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services.Scheduling.Tasks;

public class DailyScheduledService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyScheduledService> logger,
    DiscordClient client,
    BotConfiguration config,
    DiscordBotService discordBotService,
    GuildMaintenanceService guildMaintenanceService,
    DiscordLogService logService,
    ConfessionsState confessionsState,
    BoluditosState boluditosState,
    MemberActivityState memberActivityState,
    HackedAccountState hackedAccountState,
    IMensajesDiariosRepository mensajesDiariosRepository,
    MensajesDiariosSettings mensajesDiariosSettings)
    : CronBackgroundService(scopeFactory, discordBotService, logger)
{
    protected override string CronExpression => "0 0 * * *";

    protected override async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        if (!Inicializado || !client.Guilds.TryGetValue(config.GuildId, out DiscordGuild? guild))
            return;

        confessionsState.ResetDailyConfessions();
        boluditosState.ResetBoluditos();
        memberActivityState.ResetDailyActiveUsers();
        hackedAccountState.PruneStale();

        await Paso(guild, "nuevos usuarios", () => guildMaintenanceService.ManageNewUsuarios(guild));
        await Paso(guild, "challenges vencidos", guildMaintenanceService.ManageChallenges);
        await Paso(guild, "usuarios activos", () => guildMaintenanceService.ManageUsuariosActivos(guild));
        await Paso(guild, "historial de XP", () => guildMaintenanceService.ManageXpUserHistory(guild));
        await Paso(guild, "purga de mensajes diarios", () => mensajesDiariosRepository.Purgar(
            RetencionMensajes.FechaMinima(DateOnly.FromDateTime(RelojServidor.Hoy), mensajesDiariosSettings.DiasRetencion)));
    }

    private Task Paso(DiscordGuild guild, string nombre, Func<Task> paso) =>
        logService.IntentarAsync(guild, $"Tarea diaria - {nombre}", paso);
}

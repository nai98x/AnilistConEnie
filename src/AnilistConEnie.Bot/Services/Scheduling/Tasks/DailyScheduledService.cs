using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services.State;
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
    ConfessionsState confessionsState,
    BoluditosState boluditosState,
    MemberActivityState memberActivityState)
    : CronBackgroundService(scopeFactory, discordBotService, logger)
{
    protected override string CronExpression => "0 0 * * *";

    protected override async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        if (!Inicializado || !client.Guilds.TryGetValue(config.GuildId, out DiscordGuild? guild))
            return;

        await guildMaintenanceService.ManageNewUsuarios(guild);
        await guildMaintenanceService.ManageChallenges();
        await guildMaintenanceService.ManageUsuariosActivos(guild);
        await guildMaintenanceService.ManageXpUserHistory(guild);
        confessionsState.ResetDailyConfessions();
        boluditosState.ResetBoluditos();
        memberActivityState.ResetDailyActiveUsers();
    }
}

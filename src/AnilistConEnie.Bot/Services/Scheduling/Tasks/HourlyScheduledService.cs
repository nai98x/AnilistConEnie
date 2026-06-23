using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services.Scheduling.Tasks;

public class HourlyScheduledService(
    IServiceScopeFactory scopeFactory,
    ILogger<HourlyScheduledService> logger,
    DiscordClient client,
    BotConfiguration config,
    DiscordBotService discordBotService,
    BehaviorHelper behaviorHelper)
    : CronBackgroundService(scopeFactory, logger)
{
    protected override string CronExpression => "0 * * * *";

    protected override async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        if (!discordBotService.Inicializado || !client.Guilds.TryGetValue(config.GuildId, out DiscordGuild? guild))
            return;

        await behaviorHelper.ManageBoosters(guild);
        await behaviorHelper.ManageBirthdayRole(guild);
        await behaviorHelper.ManageAniversaries(guild);
        await behaviorHelper.ManageUnlinkedAccounts(client);
    }
}

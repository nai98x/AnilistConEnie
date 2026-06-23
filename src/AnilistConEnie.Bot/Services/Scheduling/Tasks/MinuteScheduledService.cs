using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services.Scheduling.Tasks;

public class MinuteScheduledService(
    IServiceScopeFactory scopeFactory,
    ILogger<MinuteScheduledService> logger,
    DiscordClient client,
    BotConfiguration config,
    DiscordBotService discordBotService,
    BehaviorHelper behaviorHelper)
    : CronBackgroundService(scopeFactory, discordBotService, logger)
{
    protected override string CronExpression => "* * * * *";

    protected override async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        if (!Inicializado || !client.Guilds.TryGetValue(config.GuildId, out DiscordGuild? guild))
            return;

        await behaviorHelper.ManageMemberXp(guild);
        await behaviorHelper.ManageInvitesRole(guild);
        await behaviorHelper.ManagePermanentUsernames(guild);
    }
}

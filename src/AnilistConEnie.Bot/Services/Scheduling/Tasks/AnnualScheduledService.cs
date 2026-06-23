using AnilistConEnie.Bot.Services.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services.Scheduling.Tasks;

public class AnnualScheduledService(
    IServiceScopeFactory scopeFactory,
    ILogger<AnnualScheduledService> logger,
    DiscordBotService discordBotService,
    XpState xpState)
    : CronBackgroundService(scopeFactory, discordBotService, logger)
{
    protected override string CronExpression => "0 0 1 1 *";

    protected override Task DoWorkAsync(CancellationToken cancellationToken)
    {
        xpState.ResetXpChartHistory();
        return Task.CompletedTask;
    }
}

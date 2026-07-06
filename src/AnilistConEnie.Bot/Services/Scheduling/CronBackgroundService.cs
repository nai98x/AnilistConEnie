using AnilistConEnie.Application.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace AnilistConEnie.Bot.Services.Scheduling;

public abstract class CronBackgroundService(IServiceScopeFactory scopeFactory, DiscordBotService discordBotService, ILogger logger) : BackgroundService
{
    protected IServiceScopeFactory ScopeFactory => scopeFactory;

    protected bool Inicializado => discordBotService.Inicializado;

    protected abstract string CronExpression { get; }

    protected abstract Task DoWorkAsync(CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (discordBotService.Debug)
        {
            logger.LogInformation("Tareas programadas deshabilitadas en modo debug ({Task})", GetType().Name);
            return;
        }

        CrontabSchedule schedule = CrontabSchedule.Parse(CronExpression);
        TimeSpan maxDelay = TimeSpan.FromMilliseconds(int.MaxValue);

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime now = RelojServidor.Ahora;
            TimeSpan delay = schedule.GetNextOccurrence(now) - now;

            while (delay > maxDelay)
            {
                await Task.Delay(maxDelay, stoppingToken);
                delay -= maxDelay;
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ejecutando la tarea programada {Task}", GetType().Name);
            }
        }
    }
}

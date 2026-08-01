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
            // Task.Delay puede despertar unos ms antes del objetivo; sin este chequeo la tarea correría
            // del lado anterior del borde (hora/día equivocados) y otra vez al instante, duplicando el trabajo.
            DateTime objetivo = schedule.GetNextOccurrence(RelojServidor.Ahora);

            TimeSpan restante;
            while ((restante = objetivo - RelojServidor.Ahora) > TimeSpan.Zero)
                await Task.Delay(restante > maxDelay ? maxDelay : restante, stoppingToken);

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

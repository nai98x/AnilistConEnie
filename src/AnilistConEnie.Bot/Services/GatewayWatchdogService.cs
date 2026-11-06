using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Services;

/// <summary>
/// Para el proceso cuando el gateway está silencioso, para que systemd lo reinicie. DSharpPlus puede
/// rendirse cuando el cliente queda en un estado fallido (ej. Discord dando 503) y deja el proceso
/// corriendo pero desconectado.
/// </summary>
/// <remarks>
/// El contador de eventos recibidos incluye los heartbeat ACK (cada ~41 segundos), entonces solo
/// se ejecuta cuando realmente no se ha recibido nada.
/// </remarks>
public sealed class GatewayWatchdogService(
    DiscordClient client,
    GatewaySettings settings,
    IHostApplicationLifetime lifetime,
    ILogger<GatewayWatchdogService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ulong lastCount = client.GetGatewayMetrics().EventsReceived;
        DateTimeOffset lastActivity = DateTimeOffset.UtcNow;

        using PeriodicTimer timer = new(settings.WatchdogCheckInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                ulong count = client.GetGatewayMetrics().EventsReceived;
                if (count != lastCount)
                {
                    lastCount = count;
                    lastActivity = DateTimeOffset.UtcNow;
                    continue;
                }

                TimeSpan silence = DateTimeOffset.UtcNow - lastActivity;
                if (silence < settings.WatchdogSilenceTimeout)
                {
                    continue;
                }

                logger.LogCritical(
                    "No se recibieron eventos del gateway durante {Silence}; apagando el proceso para que se reinicie",
                    silence);
                Environment.ExitCode = 1;
                lifetime.StopApplication();
                return;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}

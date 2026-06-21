using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Services;

public class DiscordBotService(DiscordClient client, ILogger<DiscordBotService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando bot de Discord");
        await client.ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Desconectando bot de Discord");
        await client.DisconnectAsync();
    }
}
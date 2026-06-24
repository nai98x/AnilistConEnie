using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Events.Handlers;

public class ZombiedHandler(ILogger<ZombiedHandler> logger)
{
    public async Task Handle(DiscordClient client, ZombiedEventArgs args)
    {
        logger.LogWarning("El cliente del bot fue marcado com zombieficiado por Discord.");
        
        await Task.CompletedTask;
    }
}

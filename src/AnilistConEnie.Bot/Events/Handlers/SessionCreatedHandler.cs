using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Events.Handlers;

public class SessionCreatedHandler(ILogger<SessionCreatedHandler> logger)
{
    public async Task Handle(DiscordClient client, SessionCreatedEventArgs args)
    {
        logger.LogInformation("Sesión creada. El cliente está listo para procesar eventos.");
        await Task.CompletedTask;
    }
}

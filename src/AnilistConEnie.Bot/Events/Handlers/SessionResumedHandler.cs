using AnilistConEnie.Bot.Configuration;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Events.Handlers;

public class SessionResumedHandler(ILogger<SessionResumedHandler> logger)
{
    public async Task Handle(DiscordClient client, SessionResumedEventArgs args)
    {
        logger.LogInformation("Sesión reanudada. El cliente vuelve a estar listo para procesar eventos.");
        await Task.CompletedTask;
    }
}

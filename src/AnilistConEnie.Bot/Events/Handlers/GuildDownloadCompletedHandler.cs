using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildDownloadCompletedHandler(IServiceProvider services, ILogger<GuildDownloadCompletedHandler> logger)
{
    public async Task Handle(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        DiscordBotService discordBotService = services.GetRequiredService<DiscordBotService>();
        BotStateService botStateService = services.GetRequiredService<BotStateService>();

        discordBotService.SetChannels();
        botStateService.SetInitialized();
        logger.LogInformation("Bot inicializado correctamente");

        /*if (!discordBotService.Debug)
        {
            await Funciones.ManageBoosters(client.Guilds[...]);
            await Funciones.ManageNewUsuarios(client.Guilds[...]);
            await Funciones.ManageUsuariosActivos(client.Guilds[...]);
            await Funciones.ManagSpamAccounts(client.Guilds[...]);
            await Funciones.ClearInvitesRoleOnStartup(client.Guilds[...]);
            await Funciones.ManageXPUserHistory(client.Guilds[...]);
        }*/

        await Task.CompletedTask;
    }
}

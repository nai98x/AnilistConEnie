using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildDownloadCompletedHandler(IServiceProvider services)
{
    public async Task Handle(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        MainService mainService = services.GetRequiredService<MainService>();
        mainService.SetChannels();
        mainService.SetInitialized();

        /*if (!Debug)
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

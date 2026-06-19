using System.Diagnostics;
using AnilistConEnie.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie;

public class Events(IServiceProvider services)
{
    public async Task MessageCreated(DiscordClient client, MessageCreatedEventArgs args)
    {
        if (args.Message.ChannelId == 1106702589359292416 && !args.Author.IsBot)
        {
            await args.Message.RespondAsync("uwu");
        }
    }

    public async Task GuildDownloadCompleted(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        MainService service = services.GetRequiredService<MainService>();
        service.SetChannels();
        
        service.SetInitialized();
        
        /*if (!Debug)
        {
            await Funciones.ManageBoosters(client.Guilds[862408834693070898]);
            await Funciones.ManageNewUsuarios(client.Guilds[862408834693070898]);
            await Funciones.ManageUsuariosActivos(client.Guilds[862408834693070898]);
            await Funciones.ManagSpamAccounts(client.Guilds[862408834693070898]);
            await Funciones.ClearInvitesRoleOnStartup(client.Guilds[862408834693070898]);
            await Funciones.ManageXPUserHistory(client.Guilds[862408834693070898]);
        }*/
    }
}

using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildDownloadCompletedHandler(IServiceProvider services, ILogger<GuildDownloadCompletedHandler> logger, BotConfiguration config, DiscordHelper discordHelper, IUsuariosAnilistRepository usuariosAnilistRepository, IUsuariosDiscordRepository usuariosDiscordRepository, ITriggersRepository triggersRepository)
{
    public async Task Handle(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        DiscordBotService discordBotService = services.GetRequiredService<DiscordBotService>();
        BotStateService botStateService = services.GetRequiredService<BotStateService>();

        discordBotService.SetChannels();

        if (!discordBotService.Debug)
        {
            #region Usuarios Anilist
            List<UsuarioAnilist> usuarios =  await usuariosAnilistRepository.GetListaUsuarios();
            botStateService.SetUsuarios(usuarios);
            logger.LogInformation("Usuarios Anilist cargados correctamente");
            #endregion
            
            #region Triggers
            List<Trigger> triggers = await triggersRepository.GetTriggers(true);
            botStateService.FillTriggers(triggers);
            logger.LogInformation("Triggers cargados correctamente");
            #endregion

            #region Usuarios Discord (xp)
            List<UserXp> ranking = await usuariosDiscordRepository.GetRanking();
            botStateService.FillGuildXp(ranking.ToDictionary(r => (ulong)r.UserId));
            logger.LogInformation("Xp de usuarios de Discord cargada correctamente");
            #endregion

            #region Teious
            List<TeiouCooldownNickname> cooldownTeious = await usuariosDiscordRepository.GetListTeiouNicknameCooldown();
            botStateService.FillTeiouFromDb(cooldownTeious);
            logger.LogInformation("Teious cargados correctamente");
            #endregion

            #region Cuentas baneadas AniList
            List<UsuarioAnilistBaneado> usersBaneados = await usuariosAnilistRepository.GetListaUsuariosBaneados();
            botStateService.FillAnilistBaneados(usersBaneados);
            logger.LogInformation("Usuarios baneados de Anilist cargados correctamente");
            #endregion

            #region Control de roles en startup
            try
            {
                DiscordGuild guild = client.Guilds[config.GuildId];
                DiscordRole coloresExtra = guild.Roles[config.Roles.ColoresExtra];
                DiscordRole noVinculadoRole = guild.Roles[config.Roles.NoVinculado];
                foreach (DiscordMember member in guild.Members.Values)
                {
                    if (!member.Roles.Any(x => x.Id == config.Roles.Miembro) && !member.Roles.Any(x => x.Id == config.Roles.NoVinculado) && !member.IsBot)
                    {
                        await member.GrantRoleAsync(noVinculadoRole);
                    }

                    if (discordHelper.RangoAPartirDe(guild, member, RangoEnum.Senpai, false))
                    {
                        if (!member.Roles.Any(x => x.Id == coloresExtra.Id))
                        {
                            await member.GrantRoleAsync(coloresExtra);
                        }
                    }
                }
            }
            catch (Exception) { /* Nothing to do */ }
            #endregion
        }
        
        botStateService.SetInitialized();
        logger.LogInformation("Bot inicializado correctamente");
        await discordBotService.Playroom.SendMessageAsync("Bot inicializado correctamente");

        /*if (!discordBotService.Debug)
        {
            await Funciones.ManageBoosters(client.Guilds[...]);
            await Funciones.ManageNewUsuarios(client.Guilds[...]);
            await Funciones.ManageUsuariosActivos(client.Guilds[...]);
            await Funciones.ManagSpamAccounts(client.Guilds[...]);
            await Funciones.ClearInvitesRoleOnStartup(client.Guilds[...]);
            await Funciones.ManageXPUserHistory(client.Guilds[...]);
            await Funciones.ManageUnlinkedAccounts(_discordClient);
            await Funciones.ManageFundadores(_discordClient);
            await Funciones.ManageUserDates();
        }*/

        await Task.CompletedTask;
    }
}

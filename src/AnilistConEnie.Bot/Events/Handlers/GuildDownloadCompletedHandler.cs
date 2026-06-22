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

public class GuildDownloadCompletedHandler(IServiceProvider services, ILogger<GuildDownloadCompletedHandler> logger, BotConfiguration config, BehaviorHelper behaviorHelper, IUsuariosAnilistRepository usuariosAnilistRepository, IUsuariosDiscordRepository usuariosDiscordRepository, ITriggersRepository triggersRepository)
{
    public async Task Handle(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        DiscordBotService discordBotService = services.GetRequiredService<DiscordBotService>();
        BotStateService botStateService = services.GetRequiredService<BotStateService>();
        DiscordGuild guild = client.Guilds[config.GuildId];

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
                DiscordRole noVinculadoRole = guild.Roles[config.Roles.NoVinculado];
                foreach (DiscordMember member in guild.Members.Values)
                {
                    if (!member.Roles.Any(x => x.Id == config.Roles.Miembro) && !member.Roles.Any(x => x.Id == config.Roles.NoVinculado) && !member.IsBot)
                    {
                        await member.GrantRoleAsync(noVinculadoRole);
                    }
                }
            }
            catch (Exception) { /* Nothing to do */ }
            #endregion

            #region Behavior
            await behaviorHelper.ClearInvitesRoleOnStartup(guild);
            await behaviorHelper.ManageBoosters(guild);
            await behaviorHelper.ManageNewUsuarios(guild);
            await behaviorHelper.ManageUsuariosActivos(guild);
            await behaviorHelper.ManageXpUserHistory(guild);
            await behaviorHelper.ManageUnlinkedAccounts(client);
            await behaviorHelper.ManageFundadores(guild);
            #endregion
        }
        
        discordBotService.SetInicializado();
        logger.LogInformation("Bot inicializado correctamente");
        await discordBotService.Playroom.SendMessageAsync($"Bot inicializado correctamente.\nDebugging: {discordBotService.Debug.ToString()}");
    }
}

using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildDownloadCompletedHandler(DiscordBotService discordBotService, BotStateService botStateService, ILogger<GuildDownloadCompletedHandler> logger, BotConfiguration config, BehaviorHelper behaviorHelper, IUsuariosAnilistRepository usuariosAnilistRepository, IUsuariosDiscordRepository usuariosDiscordRepository, ITriggersRepository triggersRepository)
{
    public async Task Handle(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        DiscordGuild guild = client.Guilds[config.GuildId];

        discordBotService.SetChannels();

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
        logger.LogInformation("Roles de invitacion limpiados correctamente");
        
        await behaviorHelper.ManageBoosters(guild);
        logger.LogInformation("Boosters cargados correctamente");
        
        await behaviorHelper.ManageNewUsuarios(guild);
        logger.LogInformation("Nuevos usuarios gestionados correctamente");
        
        await behaviorHelper.ManageUsuariosActivos(guild);
        logger.LogInformation("Usuarios activos gestionados correctamente");
        
        await behaviorHelper.ManageUnlinkedAccounts(client);
        logger.LogInformation("Cuentas sin vincular gestionadas correctamente");
        
        await behaviorHelper.ManageFundadores(guild);
        logger.LogInformation("Fundadores gestionados correctamente");
        
        if (config.ManageXpUserHistory)
        {
            await behaviorHelper.ManageXpUserHistory(guild);
            logger.LogInformation("Historial de XP de usuarios gestionado correctamente");
        }
        #endregion
        
        discordBotService.SetInicializado();
        logger.LogInformation("Bot inicializado correctamente");
        await discordBotService.Playroom.SendMessageAsync($"Bot inicializado correctamente.\nDebugging: {discordBotService.Debug.ToString()}");
    }
}

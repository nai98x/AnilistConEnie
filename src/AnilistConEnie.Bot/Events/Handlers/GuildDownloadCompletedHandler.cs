using System.Diagnostics;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildDownloadCompletedHandler(DiscordBotService discordBotService, TriggersState triggersState, XpState xpState, FechaEntradaState fechaEntradaState, ILogger<GuildDownloadCompletedHandler> logger, BotConfiguration config, DiscordLogService logService, GuildMaintenanceService guildMaintenanceService, IXpUsuariosRepository xpUsuariosRepository, IUsuariosRepository usuariosRepository, ITriggersRepository triggersRepository, DbConnectionFactory connectionFactory)
{
    public async Task Handle(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        Stopwatch sw = Stopwatch.StartNew();
        DiscordGuild guild = client.Guilds[config.GuildId];

        discordBotService.SetChannels();

        #region Guard de conexión a la base de datos
        try
        {
            await connectionFactory.EnsureConnectionAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "No se pudo conectar a la base de datos relacional; se aborta la inicialización del bot");
            discordBotService.SetErrorInicializacion();
            await discordBotService.Playroom.SendMessageAsync(
                "⚠️ **No se pudo conectar a la base de datos relacional.** No se cargaron triggers, XP ni se ejecutó el mantenimiento. " +
                "Revisar la conexión (¿IP del servidor / túnel?).");
            return;
        }
        #endregion

        #region Triggers
        List<Trigger> triggers = await triggersRepository.GetLista();
        triggersState.FillTriggers(triggers);
        logger.LogInformation("Triggers cargados correctamente");
        #endregion

        #region Usuarios Discord (xp)
        List<UserXp> ranking = await xpUsuariosRepository.ObtenerRanking();
        xpState.FillGuildXp(ranking.ToDictionary(r => (ulong)r.UserId));
        logger.LogInformation("Xp de usuarios de Discord cargada correctamente");
        #endregion

        #region Fechas de entrada reales
        fechaEntradaState.FillFechasEntrada(await usuariosRepository.GetFechasEntrada());
        logger.LogInformation("Fechas de entrada reales cargadas correctamente");
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
        catch (Exception ex) { await logService.LogException(guild, ex, "Control de roles en startup"); }
        #endregion

        #region Behavior
        await guildMaintenanceService.ClearInvitesRoleOnStartup(guild);
        logger.LogInformation("Roles de invitacion limpiados correctamente");
        
        guildMaintenanceService.ManageBoosters(guild);
        logger.LogInformation("Boosters cargados correctamente");
        
        await guildMaintenanceService.ManageNewUsuarios(guild);
        logger.LogInformation("Nuevos usuarios gestionados correctamente");
        
        await guildMaintenanceService.ManageUsuariosActivos(guild);
        logger.LogInformation("Usuarios activos gestionados correctamente");
        
        await guildMaintenanceService.ManageUnlinkedAccounts(client);
        logger.LogInformation("Cuentas sin vincular gestionadas correctamente");
        
        await guildMaintenanceService.ManageFundadores(guild);
        logger.LogInformation("Fundadores gestionados correctamente");
        #endregion
        
        discordBotService.SetInicializado();
        sw.Stop();
        logger.LogInformation("Bot inicializado correctamente");

        List<DiscordComponent> componentes =
        [
            new DiscordTextDisplayComponent("# ✅ Bot inicializado correctamente"),
            new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Large),
            new DiscordTextDisplayComponent(
                $"⏱️ **Tiempo de inicialización:** {sw.Elapsed.TotalSeconds:0.00} s\n" +
                $"⚡ **Triggers:** {triggers.Count}\n" +
                $"👥 **Usuarios con XP:** {ranking.Count}\n" +
                $"🧑‍🤝‍🧑 **Miembros:** {guild.MemberCount}")
        ];

        if (discordBotService.Debug)
        {
            componentes.Add(new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small));
            componentes.Add(new DiscordTextDisplayComponent("-# 🐛 Modo debug activado"));
        }

        DiscordContainerComponent container = new(componentes, color: DiscordEmojiHelper.GetColor());

        DiscordMessageBuilder builder = new DiscordMessageBuilder()
            .EnableV2Components()
            .AddContainerComponent(container);

        await Task.Delay(500);
        await discordBotService.Playroom.SendMessageAsync(builder);
    }
}

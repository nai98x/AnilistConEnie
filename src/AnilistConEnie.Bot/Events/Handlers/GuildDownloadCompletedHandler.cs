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

public class GuildDownloadCompletedHandler(DiscordBotService discordBotService, TriggersState triggersState, ILogger<GuildDownloadCompletedHandler> logger, BotConfiguration config, DiscordLogService logService, GuildMaintenanceService guildMaintenanceService, ITriggersRepository triggersRepository, DbConnectionFactory connectionFactory, YumikoDbConnectionFactory yumikoConnectionFactory)
{
    public async Task Handle(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        try
        {
            await InicializarAsync(client);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Falló la inicialización del bot");
            discordBotService.SetErrorInicializacion();

            if (client.Guilds.TryGetValue(config.GuildId, out DiscordGuild? guild)
                && guild.Channels.TryGetValue(config.Channels.Playroom, out DiscordChannel? playroom))
            {
                await playroom.SendMessageAsync("⚠️ **Falló la inicialización del bot.** Los comandos no están disponibles; revisar los logs.");
            }
        }
    }

    private async Task InicializarAsync(DiscordClient client)
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

        #region Guard de conexión a la base de Yumiko
        // No es crítica: sin ella solo se pierde el espejo del vínculo, el bot arranca igual.
        bool yumikoOk = true;
        try
        {
            await yumikoConnectionFactory.EnsureConnectionAsync();
        }
        catch (Exception ex)
        {
            yumikoOk = false;
            logger.LogError(ex, "No se pudo conectar a la base de Yumiko; los vínculos de AniList no se van a espejar");
            await logService.LogException(guild, ex, "Guard de conexión a la base de Yumiko");
        }
        #endregion

        bool miembrosCompletos = await guildMaintenanceService.CargarMiembros(guild);
        if (miembrosCompletos)
            logger.LogInformation("Miembros del servidor cargados: {Cantidad}", guild.Members.Count);

        List<string> pasosFallidos = [];

        async Task Paso(string nombre, Func<Task> paso)
        {
            if (await logService.IntentarAsync(guild, $"Inicialización - {nombre}", paso))
                logger.LogInformation("Inicialización - {Paso}: correcto", nombre);
            else
                pasosFallidos.Add(nombre);
        }

        await Paso("Triggers", async () => triggersState.FillTriggers(await triggersRepository.GetLista()));
        await Paso("Fechas de entrada", () => guildMaintenanceService.ManageFechasEntrada(guild));
        await Paso("Roles de invitación", () => guildMaintenanceService.ClearInvitesRoleOnStartup(guild));
        await Paso("Nuevos usuarios", () => guildMaintenanceService.ManageNewUsuarios(guild));

        // Sin la lista completa, un miembro offline pasaría por ausente y se lo desvincularía.
        if (miembrosCompletos)
            await Paso("Usuarios ausentes", () => guildMaintenanceService.ManageUsuariosAusentes(guild));

        await Paso("Usuarios activos", () => guildMaintenanceService.ManageUsuariosActivos(guild));
        await Paso("Rol activo en comunidad", () => guildMaintenanceService.ManageRolActivoEnComunidad(guild));
        await Paso("Cuentas sin vincular", () => guildMaintenanceService.ManageUnlinkedAccounts(client));
        await Paso("Fundadores", () => guildMaintenanceService.ManageFundadores(guild));

        discordBotService.SetInicializado();
        sw.Stop();
        logger.LogInformation("Bot inicializado correctamente");

        List<DiscordComponent> componentes =
        [
            new DiscordTextDisplayComponent("# ✅ Bot inicializado correctamente"),
            new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Large),
            new DiscordTextDisplayComponent(
                $"⏱️ **Tiempo de inicialización:** {sw.Elapsed.TotalSeconds:0.00} s\n" +
                $"⚡ **Triggers:** {triggersState.GetActiveTriggers().Count}\n" +
                $"🧑‍🤝‍🧑 **Miembros:** {guild.MemberCount}")
        ];

        if (!miembrosCompletos)
        {
            componentes.Add(new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small));
            componentes.Add(new DiscordTextDisplayComponent(
                "⚠️ **No se pudo cargar la lista completa de miembros:** los miembros desconectados pueden faltar en rankings y listados hasta el próximo reinicio."));
        }

        if (pasosFallidos.Count > 0)
        {
            componentes.Add(new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small));
            componentes.Add(new DiscordTextDisplayComponent(
                $"⚠️ **Pasos de inicialización con error:** {string.Join(", ", pasosFallidos)}. El detalle está en el canal de errores."));
        }

        if (!yumikoOk)
        {
            componentes.Add(new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small));
            componentes.Add(new DiscordTextDisplayComponent(
                "⚠️ **Sin conexión a la base de Yumiko:** los vínculos de AniList no se van a espejar."));
        }

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

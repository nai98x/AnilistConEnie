using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using AnilistConEnie.Application.Actividad;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Entities.Anilist;
using AnilistConEnie.Domain.Exceptions;
using AnilistConEnie.Domain.Interfaces;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Commands.Slash;

[Command("owner")]
[TestCommand]
[RequirePermissions(DiscordPermission.Administrator)]
public class Owner(XpState xpState, PermanentUsernameState permanentUsernameState, FechaEntradaState fechaEntradaState, DiscordBotService discordBotService, IAnilistClient anilistClient, IHostApplicationLifetime appLifetime, BotConfiguration config, IXpUsuariosRepository xpUsuariosRepository, IUsuariosRepository usuariosRepository, IMensajesDiariosRepository mensajesDiariosRepository, MensajesDiariosSettings mensajesDiariosSettings, ILogger<Owner> logger)
{
    [Command("test")]
    [Description("Comando general para testear cosas")]
    public async Task TestCommand(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Test"));
    }

    [Command("testv2")]
    [Description("Showcase de Components V2 (container, section, gallery, separator)")]
    public async Task TestComponentsV2(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        DiscordContainerComponent container = new(
            components:
            [
                new DiscordTextDisplayComponent(
                    "# Components V2\n" +
                    "Esto es un **container** con borde de color. Adentro van todos los demás " +
                    "componentes; el texto va en `TextDisplay`, ya no en `content` ni embeds."),

                new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Large),

                new DiscordSectionComponent(
                    text: "## Section con thumbnail\n" +
                          "Una *section* es texto a la izquierda y un **accessory** a la derecha " +
                          "(thumbnail o botón). Acá va una imagen.",
                    accessory: new DiscordThumbnailComponent(
                        "https://media.discordapp.net/attachments/879612956848062514/879628082921762826/imagen_2021-07-15_150953_1.png",
                        description: "Logo")),

                new DiscordSeparatorComponent(divider: false, spacing: DiscordSeparatorSpacing.Small),

                new DiscordSectionComponent(
                    text: "## Section con botón\n" +
                          "El accessory también puede ser un botón interactivo.",
                    accessory: new DiscordButtonComponent(
                        DiscordButtonStyle.Primary, "owner-testv2-boton", "Tocame")),

                new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small),

                new DiscordTextDisplayComponent("## Media gallery"),
                new DiscordMediaGalleryComponent(
                [
                    new DiscordMediaGalleryItem(
                        "https://media.discordapp.net/attachments/879612956848062514/879628082921762826/imagen_2021-07-15_150953_1.png",
                        description: "Imagen 1"),
                    new DiscordMediaGalleryItem(
                        "https://media.discordapp.net/attachments/879612956848062514/879628082921762826/imagen_2021-07-15_150953_1.png",
                        description: "Imagen 2")
                ])
            ],
            color: DiscordEmojiHelper.GetColor());

        DiscordMessageBuilder builder = new DiscordMessageBuilder()
            .EnableV2Components()
            .AddContainerComponent(container)
            .AddActionRowComponent(
                new DiscordLinkButtonComponent("https://anilist.co", "Botón top-level (fuera del container)"));

        await ctx.EditResponseAsync(builder);
    }

    [Command("reiniciar")]
    [Description("Reinicia el bot")]
    public async Task Restart(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();
        await ctx.EditResponseAsync("Reiniciando el bot...");

        appLifetime.StopApplication();
    }

    [Command("test_db")]
    [Description("Prueba la conexión a la base de datos relacional")]
    public async Task TestDb(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            UserXp? xp = await xpUsuariosRepository.Obtener(ctx.User.Id);
            sw.Stop();
            await ctx.EditResponseAsync(
                $"✅ Conexión OK ({sw.ElapsedMilliseconds} ms). Lectura de prueba: " +
                $"{(xp is null ? "sin registro propio (consulta ejecutada igual)" : $"total {xp.Total} XP")}.");
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Falló el test de conexión a la base de datos");
            await ctx.EditResponseAsync($"❌ Falló la conexión ({sw.ElapsedMilliseconds} ms): {ex.Message}");
        }
    }

    [Command("debugxpenable")]
    [Description("Enable Debug de xp")]
    public async Task EnableDebugXp(SlashCommandContext ctx, [Parameter("Usuario")] [Description("El usuario del que quieres debuggear su xp")] DiscordUser usuario)
    {
        await ctx.DeferEphemeralAsync();
        
        xpState.EnableDebugXp(usuario.Id);

        await discordBotService.Playroom.SendMessageAsync($"Empezo el debug de xp del usuario {usuario.Username}");
        await ctx.DeleteResponseAsync();
    }

    [Command("debugxpdisable")]
    [Description("Disable Debug de xp")]
    public async Task DisableDebugXp(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        xpState.DisableDebugXp();

        await discordBotService.Playroom.SendMessageAsync("Finalizo el debug de xp");
        await ctx.DeleteResponseAsync();
    }
    
    [Command("setpermanentusername")]
    [Description("Set permanent username")]
    public async Task SetPermanentUsername(
        SlashCommandContext ctx, 
        [Parameter("Usuario")] [Description("El usuario del que quieres que tenga el nickname permanente")] DiscordMember member,
        [Parameter("Username")] [Description("El nickname")] string username)
    {
        await ctx.DeferEphemeralAsync();
        permanentUsernameState.SetPermanentUsername(member.Id, username);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"El usuario {member.Username} ahora tiene el nickname permanente '{username}'")
            .AsEphemeral());
    }

    [Command("removepermanentusername")]
    [Description("Remove permanent username")]
    public async Task RemovePermanentUsername(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario del que quieres que tenga el nickname permanente")] DiscordMember member)
    {
        await ctx.DeferEphemeralAsync();
        permanentUsernameState.RemovePermanentUsername(member.Id);
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"El usuario {member.Username} ya no tiene el nickname permanente")
            .AsEphemeral());
    }
    
    [Command("set_fecha_entrada_usuario")]
    [Description("Registra la fecha de entrada real de un usuario al servidor (hora de Argentina)")]
    public async Task SetFechaEntradaUsuario(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario al que se le registra la fecha de entrada")] DiscordUser usuario,
        [Parameter("Fecha")] [Description("Fecha y hora de entrada en hora de Argentina (dd/MM/yyyy HH:mm)")] string fecha)
    {
        await ctx.DeferEphemeralAsync();

        if (!DateTime.TryParseExact(fecha.Trim(), ["dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaLocal))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"Fecha `{fecha}` invalida (debe ser dd/MM/yyyy HH:mm)")));
            return;
        }

        DateTime fechaUtc = RelojServidor.DesdeHoraLocal(fechaLocal);
        if (fechaUtc > DateTime.UtcNow)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("La fecha de entrada no puede ser futura.")));
            return;
        }

        if (!await usuariosRepository.SetFechaEntrada(usuario.Id, fechaUtc))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"{usuario.Username} no está registrado en la base de datos.")));
            return;
        }

        fechaEntradaState.SetFechaEntrada(usuario.Id, fechaUtc);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Fecha de entrada de {usuario.Username} registrada: {fechaLocal:dd/MM/yyyy HH:mm} ({Formatter.Timestamp(new DateTimeOffset(fechaUtc), TimestampFormat.LongDateTime)})"));
    }

    [Command("rol_activo_comunidad")]
    [Description("Muestra si un usuario tiene el rol activo en la comunidad y cómo va con cada condición")]
    public async Task RolActivoComunidadUsuario(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario a consultar")] DiscordUser usuario)
    {
        await ctx.DeferEphemeralAsync();

        DiscordMember miembro;
        try
        {
            miembro = await ctx.Guild!.GetMemberAsync(usuario.Id);
        }
        catch (NotFoundException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De($"{usuario.Mention} no se encuentra en el servidor")));
            return;
        }

        MensajesDiariosSettings s = mensajesDiariosSettings;
        (DateOnly desde, DateOnly hasta) = RolActivoComunidad.Ventana(DateOnly.FromDateTime(RelojServidor.Hoy), s.RolActivoVentanaDias);
        List<UsuarioMensajes> totales = await mensajesDiariosRepository.ObtenerTotales(desde, hasta, config.Channels.General);
        UsuarioMensajes total = totales.FirstOrDefault(x => x.UserId == (long)miembro.Id) ?? new UsuarioMensajes { UserId = (long)miembro.Id };

        DateTimeOffset fechaEntrada = fechaEntradaState.GetFechaEntrada(miembro.Id, miembro.JoinedAt);
        DateTimeOffset cumpleAntiguedad = fechaEntrada.AddMonths(s.RolActivoAntiguedadMinimaMeses);
        EvaluacionRolActivo evaluacion = RolActivoComunidad.Evaluar(total.Total, total.MaximoEnUnCanal, total.Dias, s.RolActivoMasDeMensajes,
            s.RolActivoMinimoFueraDelCanalPrincipal, s.RolActivoMinimoDias, fechaEntrada, DateTimeOffset.UtcNow, s.RolActivoAntiguedadMinimaMeses);
        bool tieneRol = miembro.Roles.Any(x => x.Id == config.Roles.ActivoEnComunidad);

        static string Check(bool cumple) => cumple ? "✅" : "❌";

        string desc =
            $"{Formatter.Bold("Tiene el rol:")} {(tieneRol ? "Sí" : "No")}\n" +
            $"{Formatter.Bold("Le corresponde:")} {(evaluacion.Corresponde ? "Sí" : "No")}\n" +
            $"Ventana: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} ({s.RolActivoVentanaDias} días, hoy incluido)\n\n" +
            $"{Check(evaluacion.Mensajes)} Mensajes: {total.Total} (más de {s.RolActivoMasDeMensajes})\n" +
            $"{Check(evaluacion.FueraDelCanalPrincipal)} Fuera del canal principal: {total.Total - total.MaximoEnUnCanal} (mínimo {s.RolActivoMinimoFueraDelCanalPrincipal}; " +
            $"canal principal sin contar general: {total.MaximoEnUnCanal})\n" +
            $"{Check(evaluacion.Dias)} Días con mensajes: {total.Dias} (mínimo {s.RolActivoMinimoDias})\n" +
            $"{Check(evaluacion.Antiguedad)} Antigüedad: entró {Formatter.Timestamp(fechaEntrada, TimestampFormat.ShortDate)}, " +
            $"cumple {s.RolActivoAntiguedadMinimaMeses} mes(es) {Formatter.Timestamp(cumpleAntiguedad, TimestampFormat.RelativeTime)}";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = $"Rol activo en la comunidad: {miembro.DisplayName}",
            Description = desc,
            Color = evaluacion.Corresponde ? DiscordColor.Green : DiscordColor.Red
        }));
    }

    [Command("configurarbienvenida")]
    [Description("Agrega el mensaje de bienvenida")]
    public async Task ConfigurarBienvenida(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        DiscordChannel channel = ctx.Guild!.Channels[config.Channels.Bienvenida];

        DiscordMessageBuilder msgBuilder = new DiscordMessageBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithThumbnail("https://media.discordapp.net/attachments/879612956848062514/879628082921762826/imagen_2021-07-15_150953_1.png")
                .WithTitle("Vincula tu AniList")
                .WithDescription(
                    "# **Instrucciones**:\n\n" +
                    "- Haz click en el botón llamado **Autorizar**\n" +
                    "- Una vez se abra la página web, haz click en el botón verde **Authorize** y luego copia el texto que te aparecerá para copiar\n" +
                    "- Cierra la página web y haz click en el botón llamado **Pegar código aquí**\n" +
                    "- Pega el código en el formulario y envíalo")
                .WithFooter("Apenas tengas tu cuenta de AniList vinculada, se te desbloquearán todos los canales del servidor.")
                .WithColor(DiscordEmojiHelper.GetColor()))
            .AddActionRowComponent(
                new DiscordLinkButtonComponent("https://anilist.co/api/v2/oauth/authorize?client_id=8655&response_type=token", "Autorizar"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "modal-anilistprofileset", "Pegar código aquí"));

        await channel.SendMessageAsync(msgBuilder);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Mensaje de bienvenida creado con exito"));
    }

    [Command("configurarcolores")]
    [Description("Agrega el mensaje de colores")]
    public async Task ConfigurarColores(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        DiscordChannel channel = ctx.Guild!.Channels[config.Channels.Colores];

        (string Rango, string Header)[] grupos =
        [
            ("Miembro", "# Colores básicos\nAccesibles para cualquier rango."),
            ("Senpai", "# Colores Avanzados\nAccesibles para rango Senpai."),
            ("Ousama", "# Colores con degradado\nAccesibles para rango Ousama."),
            ("Teiou", "# Colores Premium\nAccesibles para rango Teiou.")
        ];

        int selectIndex = 0;
        foreach ((string rango, string header) in grupos)
        {
            List<BotConfiguration.ColorRangoConfiguration> colores = config.Roles.ColoresRango.Where(x => x.Rango == rango).ToList();
            if (colores.Count == 0) continue;

            DiscordMessageBuilder msgBuilder = new DiscordMessageBuilder().WithContent(header);

            foreach (BotConfiguration.ColorRangoConfiguration[] chunk in colores.Chunk(25))
            {
                List<DiscordSelectComponentOption> options = chunk
                    .Select(c => new DiscordSelectComponentOption(c.Nombre, c.RoleId.ToString()))
                    .ToList();
                msgBuilder.AddActionRowComponent(new DiscordSelectComponent($"colores{++selectIndex}", "Selecciona un color", options));
            }

            await channel.SendMessageAsync(msgBuilder);
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Mensaje de colores creado con exito"));
    }

    [Command("ratelimits")]
    [Description("Muestra los ratelimits de APIs que interactual con el bot")]
    public async Task Ratelimits(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        AnilistRateLimit rateLimit;
        try
        {
            rateLimit = await anilistClient.GetRateLimitAsync();
        }
        catch (AnilistServerErrorException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AnilistErrorEmbed.NoDisponible()));
            return;
        }

        string desc = $"{Formatter.Bold("AniList:")}\n" +
                      $"Limit: {rateLimit.Limit?.ToString() ?? "?"}\n" +
                      $"Remaining: {rateLimit.Remaining?.ToString() ?? "?"}";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Rate limits",
            Description = desc,
            Color = DiscordEmojiHelper.GetColor()
        }));
    }

    [Command("logs")]
    [Description("Adjunta los logs del día de hoy como archivo de texto")]
    public async Task Logs(SlashCommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        string carpeta = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        string patron = $"anilistconenie-{DateTime.Now:yyyyMMdd}*.log";

        string[] archivos = Directory.Exists(carpeta)
            ? Directory.GetFiles(carpeta, patron).OrderBy(x => x).ToArray()
            : [];

        if (archivos.Length == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No hay logs del día de hoy."));
            return;
        }

        List<MemoryStream> streams = [];
        DiscordWebhookBuilder builder = new DiscordWebhookBuilder()
            .WithContent($"Logs del {DateTime.Now:dd-MM-yyyy} ({archivos.Length} archivo/s).");

        foreach (string archivo in archivos)
        {
            MemoryStream ms = new();
            await using (FileStream fs = new(archivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                await fs.CopyToAsync(ms);
            ms.Position = 0;
            streams.Add(ms);
            builder.AddFile(Path.GetFileName(archivo), ms);
        }

        try
        {
            await ctx.EditResponseAsync(builder);
        }
        finally
        {
            foreach (MemoryStream ms in streams)
                await ms.DisposeAsync();
        }
    }

}
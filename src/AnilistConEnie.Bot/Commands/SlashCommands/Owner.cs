using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Interfaces;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[Command("owner")]
[TestCommand]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class Owner(XpState xpState, PermanentUsernameState permanentUsernameState, DiscordBotService discordBotService, IAnilistClient anilistClient, IHostApplicationLifetime appLifetime, BotConfiguration config)
{
    [Command("test")]
    [Description("Comando general para testear cosas")]
    public async Task TestCommand(CommandContext ctx, [Description("Test input 1")]string input)
    {
        await ctx.DeferEphemeralAsync();

        List<ulong> ids =
        [
            ..config.FechasEntradaExcepciones.Select(x => x.UserId),
            638190435835183117
        ];

        List<string> lineas = [];
        foreach (ulong id in ids)
        {
            DateTimeOffset real;
            try
            {
                DiscordMember member = await ctx.Guild!.GetMemberAsync(id);
                real = member.JoinedAt;
            }
            catch
            {
                lineas.Add($"`{id}`: no se pudo obtener el miembro");
                continue;
            }

            DateTimeOffset resuelta = config.GetFechaEntrada(id, real);
            bool esExcepcion = resuelta != real;

            lineas.Add(
                $"`{id}` {(esExcepcion ? "✅ excepción" : "➖ fecha real")}\n" +
                $"- Real: {real:yyyy/MM/dd HH:mm:ss}\n" +
                $"- Resuelta: {resuelta:yyyy/MM/dd HH:mm:ss}");
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Test GetFechaEntrada",
            Description = string.Join("\n\n", lineas),
            Color = DiscordHelper.GetColor()
        }));
    }
    
    [Command("apagar")]
    [Description("Apaga el bot")]
    public async Task Shutdown(CommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();
        await ctx.EditResponseAsync($"Apagando el bot...");

        appLifetime.StopApplication();
    }

    [Command("debugxpenable")]
    [Description("Enable Debug de xp")]
    public async Task EnableDebugXp(CommandContext ctx, [Parameter("Usuario")] [Description("El usuario del que quieres debuggear su xp")] DiscordUser usuario)
    {
        await ctx.DeferEphemeralAsync();
        
        xpState.EnableDebugXp(usuario.Id);

        await discordBotService.Playroom.SendMessageAsync($"Empezo el debug de xp del usuario {usuario.Username}");
        await ctx.DeleteResponseAsync();
    }

    [Command("debugxpdisable")]
    [Description("Disable Debug de xp")]
    public async Task DisableDebugXp(CommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        xpState.DisableDebugXp();

        await discordBotService.Playroom.SendMessageAsync("Finalizo el debug de xp");
        await ctx.DeleteResponseAsync();
    }
    
    [Command("setpermanentusername")]
    [Description("Set permanent username")]
    public async Task SetPermanentUsername(
        CommandContext ctx, 
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
        CommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario del que quieres que tenga el nickname permanente")] DiscordMember member)
    {
        await ctx.DeferEphemeralAsync();
        permanentUsernameState.RemovePermanentUsername(member.Id);
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"El usuario {member.Username} ya no tiene el nickname permanente")
            .AsEphemeral());
    }
    
    [Command("configurarbienvenida")]
    [Description("Agrega el mensaje de bienvenida")]
    public async Task ConfigurarBienvenida(CommandContext ctx)
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
                .WithColor(DiscordHelper.GetColor()))
            .AddActionRowComponent(
                new DiscordLinkButtonComponent("https://anilist.co/api/v2/oauth/authorize?client_id=8655&response_type=token", "Autorizar"),
                new DiscordButtonComponent(DiscordButtonStyle.Primary, "modal-anilistprofileset", "Pegar código aquí"));

        await channel.SendMessageAsync(msgBuilder);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Mensaje de bienvenida creado con exito"));
    }

    [Command("configurarcolores")]
    [Description("Agrega el mensaje de colores")]
    public async Task ConfigurarColores(CommandContext ctx)
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
    public async Task Ratelimits(CommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        AnilistRateLimit rateLimit = await anilistClient.GetRateLimitAsync();

        string desc = $"{Formatter.Bold("AniList:")}\n" +
                      $"Limit: {rateLimit.Limit?.ToString() ?? "?"}\n" +
                      $"Remaining: {rateLimit.Remaining?.ToString() ?? "?"}";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Rate limits",
            Description = desc,
            Color = DiscordHelper.GetColor()
        }));
    }

}
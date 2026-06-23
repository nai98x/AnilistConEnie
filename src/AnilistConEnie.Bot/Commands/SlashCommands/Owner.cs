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
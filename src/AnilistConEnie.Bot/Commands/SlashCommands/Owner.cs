using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;
using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[Command("owner")]
[TestCommand]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class Owner(BotStateService botStateService, DiscordBotService  discordBotService)
{
    [Command("test")]
    [Description("Comando general para testear cosas")]
    public static async Task TestCommand(CommandContext ctx, [Description("Test input 1")]string input)
    {
        await ctx.DeferEphemeralAsync();
        await ctx.EditResponseAsync($"Test input: {input}");
    }
    
    [Command("apagar")]
    [Description("Apaga el bot")]
    public async Task Shutdown(CommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();
        await ctx.EditResponseAsync($"Apagando el bot...");

        Environment.Exit(0);
    }

    [Command("debugxpenable")]
    [Description("Enable Debug de xp")]
    public async Task EnableDebugXp(CommandContext ctx, [Parameter("Usuario")] [Description("El usuario del que quieres debuggear su xp")] DiscordUser usuario)
    {
        await ctx.DeferEphemeralAsync();
        
        botStateService.EnableDebugXp(usuario.Id);

        await discordBotService.Playroom.SendMessageAsync($"Empezo el debug de xp del usuario {usuario.Username}");
        await ctx.DeleteResponseAsync();
    }

    [Command("debugxpdisable")]
    [Description("Disable Debug de xp")]
    public async Task DisableDebugXp(CommandContext ctx)
    {
        await ctx.DeferEphemeralAsync();

        botStateService.DisableDebugXp();

        await discordBotService.Playroom.SendMessageAsync("Finalizo el debug de xp");
        await ctx.DeleteResponseAsync();
    }
    
    [Command("setpermanentusername")]
    [Description("Set permanent username")]
    public async Task SetPermanentUsername(
        CommandContext ctx, 
        [Parameter("Usuario")] [Description("El usuario del que quieres que tenga el nickname permanente")] DiscordUser user,
        [Parameter("Username")] [Description("El nickname")] string username)
    {
        await ctx.DeferEphemeralAsync();
        botStateService.SetPermanentUsername(user.Id, username);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"El usuario {user.Username} ahora tiene el nickname permanente '{username}'")
            .AsEphemeral());
    }

    [Command("removepermanentusername")]
    [Description("Remove permanent username")]
    public async Task RemovePermanentUsername(
        CommandContext ctx,
        [Parameter("Usuario")] [Description("El usuario del que quieres que tenga el nickname permanente")] DiscordUser user)
    {
        await ctx.DeferEphemeralAsync();
        botStateService.RemovePermanentUsername(user.Id);
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"El usuario {user.Username} ya no tiene el nickname permanente")
            .AsEphemeral());
    }
}
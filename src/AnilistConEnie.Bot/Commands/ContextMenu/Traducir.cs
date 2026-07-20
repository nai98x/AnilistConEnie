using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Commands.ContextMenu;

public class Traducir(
    IHttpClientFactory httpClientFactory,
    DiscordBotService discordBotService)
{
    [Command("Traducir")]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public async Task TraducirMensaje(MessageCommandContext ctx, DiscordMessage targetMessage)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        DiscordMember member = await ctx.Guild!.GetMemberAsync(targetMessage.Author!.Id);

        HttpClient client = httpClientFactory.CreateClient();
        string translated = await TranslationHelper.TranslateAsync(client, targetMessage.Content, "auto", "es");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Mensaje Original")
                .WithDescription(targetMessage.Content)
                .WithAuthor(member.DisplayName, null, member.AvatarUrlPreferido()))
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Traduccion")
                .WithDescription(translated)
                .WithColor(DiscordColor.Green)));
    }
}

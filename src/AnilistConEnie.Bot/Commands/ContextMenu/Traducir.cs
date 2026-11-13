using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Commands.ContextMenu;

public class Traducir(
    IHttpClientFactory httpClientFactory,
    DiscordBotService discordBotService,
    TraduccionSettings traduccionSettings,
    ILogger<Traducir> logger)
{
    [Command("Traducir")]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public async Task TraducirMensaje(MessageCommandContext ctx, DiscordMessage targetMessage)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync(true);

        DiscordMember member = await ctx.Guild!.GetMemberAsync(targetMessage.Author!.Id);

        HttpClient client = httpClientFactory.CreateClient();
        string translated;
        try
        {
            translated = await TranslationHelper.TranslateAsync(client, traduccionSettings.Url, targetMessage.Content, "auto", "es");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo traducir el mensaje {MessageId}", targetMessage.Id);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("El traductor no está disponible en este momento. Intenta de nuevo más tarde.")));
            return;
        }

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

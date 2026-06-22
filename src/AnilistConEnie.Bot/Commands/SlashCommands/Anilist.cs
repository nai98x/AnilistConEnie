using System.ComponentModel;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Interfaces;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[TestCommand]
public class Anilist(IAnilistClient anilistClient, AnilistHelper anilistHelper)
{
    // Límite de caracteres de la description de un embed de Discord.
    private const int EmbedDescriptionLimit = 4096;

    [Command("statsserver")]
    [Description("Estadisticas de los usuarios del servidor de un anime o manga en AniList")]
    public async Task StatsServer(
        SlashCommandContext ctx,
        [Parameter("Nombre")] [Description("Nombre del anime o manga a buscar")] string mediaNombre,
        [Parameter("Tipo")] [Description("Elige si buscas anime o manga")] AnilistMediaType tipo)
    {
        await ctx.DeferResponseAsync();

        IReadOnlyList<AnilistMedia> resultados = await anilistClient.SearchMediaAsync(mediaNombre, tipo);

        if (resultados.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"No se encontró el {tipo.ToString().ToLowerInvariant()} `{mediaNombre}`"));
            return;
        }

        List<TitleDescription> opciones = resultados.Select(m => new TitleDescription
        {
            Title = AnilistMediaFormatter.DisplayTitle(m),
            Description = AnilistMediaFormatter.OptionSubtitle(m)
        }).ToList();

        int elegido = await DiscordHelper.GetElegidoAsync(ctx, 60, opciones);
        if (elegido <= 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Tiempo agotado esperando la selección."));
            return;
        }

        AnilistMedia media = resultados[elegido - 1];

        // Aviso intermedio: recorrer los miembros y consultar Aniville por lotes puede demorar.
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = $"Buscando scores de {AnilistMediaFormatter.DisplayTitle(media)}…",
            Description = "Esto puede demorar unos segundos.",
            Color = DiscordHelper.GetColor()
        }));

        string scores = await anilistHelper.GetServerScoresAsync(ctx.Guild!, media, includeUsersWithoutScore: true);

        // Embed base sin description: se usa tal cual (página única) o como plantilla de cada página.
        DiscordEmbedBuilder embed = new()
        {
            Title = media.IsAdult
                ? $"Scores de {AnilistMediaFormatter.DisplayTitle(media)} [NSFW] en {ctx.Guild!.Name}"
                : $"Scores de {AnilistMediaFormatter.DisplayTitle(media)} en {ctx.Guild!.Name}",
            Url = media.SiteUrl,
            Color = DiscordHelper.GetColor()
        };

        if (!media.IsAdult || ctx.Channel.IsNSFW || (ctx.Channel.Parent?.IsNSFW ?? false))
        {
            if (!string.IsNullOrEmpty(media.CoverImageUrl)) embed.WithThumbnail(media.CoverImageUrl);
            if (!string.IsNullOrEmpty(media.BannerImageUrl)) embed.WithImageUrl(media.BannerImageUrl);
        }

        if (media.Format is { Length: > 0 })
            embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":dividers:")} Formato", StringHelper.NormalizarField(media.Format), true);

        embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":hourglass_flowing_sand:")} Estado", AnilistMediaFormatter.Status(media), true);
        embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":calendar_spiral:")} Fecha", AnilistMediaFormatter.Dates(media), false);

        if (string.IsNullOrEmpty(scores))
        {
            embed.Description = $"Todavía nadie tiene **{AnilistMediaFormatter.DisplayTitle(media)}** en su lista.";
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            return;
        }

        // Si entra en una sola description, embed simple. Si no, paginamos partiendo por líneas.
        if (scores.Length <= EmbedDescriptionLimit)
        {
            embed.Description = scores;
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            return;
        }

        IEnumerable<Page> pages = InteractivityExtension.GeneratePagesInEmbed(scores, SplitType.Line, embed);
        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, ephemeral: false, ctx.User, pages);
    }
}

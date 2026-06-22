using System.ComponentModel;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Interfaces;
using DSharpPlus.Commands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[TestCommand]
public class Anilist(IAnilistClient anilistClient)
{
    [Command("statsserver")]
    [Description("Estadisticas de los usuarios del servidor de un anime o manga en AniList")]
    public async Task StatsServer(
        CommandContext ctx,
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

        // TODO: selector entre los varios resultados (lo que antes hacía GetElegido). Por ahora se
        // toma el primero (el más relevante según AniList).
        AnilistMedia media = resultados[0];
        
        DiscordEmbedBuilder embed = new()
        {
            Title = media.IsAdult
                ? $"{AnilistMediaFormatter.DisplayTitle(media)} [NSFW]"
                : AnilistMediaFormatter.DisplayTitle(media),
            Url = media.SiteUrl,
            Description = AnilistMediaFormatter.Description(media),
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
        embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":star:")} Score", AnilistMediaFormatter.Score(media), true);
        embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":calendar_spiral:")} Fecha", AnilistMediaFormatter.Dates(media), false);

        if (media.Genres.Count > 0)
            embed.AddField("Géneros", AnilistMediaFormatter.Genres(media), false);

        // TODO: feature original de "statsserver" — calcular y mostrar los scores de los usuarios del
        // servidor para este media (lo que antes hacía GetScoreMediaUsuarios), con paginación si excede
        // el límite del embed.

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }
}

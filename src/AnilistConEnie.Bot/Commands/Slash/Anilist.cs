using System.ComponentModel;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Exceptions;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;

namespace AnilistConEnie.Bot.Commands.Slash;

//[TestCommand]
[Command("anilist")]
public class Anilist(IAnilistClient anilistClient, AnilistService anilistService, IUsuariosRepository usuariosRepository, BotConfiguration config, DiscordBotService discordBotService)
{
    [Command("statsserver")]
    [Description("Estadisticas de los usuarios del servidor de un anime o manga en AniList")]
    public async Task StatsServer(
        SlashCommandContext ctx,
        [Parameter("Nombre")] [Description("Nombre del anime o manga a buscar")] string mediaNombre,
        [Parameter("Tipo")] [Description("Elige si buscas anime o manga")] AnilistMediaType tipo)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        IReadOnlyList<AnilistMedia> resultados;
        try
        {
            resultados = await anilistClient.SearchMediaAsync(mediaNombre, tipo);
        }
        catch (AnilistServerErrorException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AnilistErrorEmbed.NoDisponible()));
            return;
        }

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

        int elegido = await DiscordInteractivity.GetElegidoAsync(ctx, 60, opciones);
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
            Color = DiscordEmojiHelper.GetColor()
        }));

        ServerScoresView view;
        try
        {
            view = await anilistService.GetServerScoresAsync(ctx.Guild!, media, includeUsersWithoutScore: true);
        }
        catch (AnilistServerErrorException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AnilistErrorEmbed.NoDisponible()));
            return;
        }

        bool mostrarImagenes = !media.IsAdult || ctx.Channel.IsNSFW || (ctx.Channel.Parent?.IsNSFW ?? false);

        DiscordEmbedBuilder BaseEmbed()
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = media.IsAdult
                    ? $"Scores de {AnilistMediaFormatter.DisplayTitle(media)} [NSFW] en {ctx.Guild!.Name}"
                    : $"Scores de {AnilistMediaFormatter.DisplayTitle(media)} en {ctx.Guild!.Name}",
                Url = media.SiteUrl,
                Color = DiscordEmojiHelper.GetColor()
            };

            if (mostrarImagenes)
            {
                if (!string.IsNullOrEmpty(media.CoverImageUrl)) embed.WithThumbnail(media.CoverImageUrl);
                if (!string.IsNullOrEmpty(media.BannerImageUrl)) embed.WithImageUrl(media.BannerImageUrl);
            }

            if (view.Average100 is { } promedio)
                embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":star:")} Promedio", $"{Math.Round(promedio, 2)}/100", true);
            if (media.Format is { Length: > 0 })
                embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":dividers:")} Formato", StringHelper.NormalizarField(media.Format), true);
            embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":hourglass_flowing_sand:")} Estado", AnilistMediaFormatter.Status(media), true);
            embed.AddField($"{DiscordEmoji.FromName(ctx.Client, ":calendar_spiral:")} Fecha", AnilistMediaFormatter.Dates(media), false);

            return embed;
        }

        if (view.IsEmpty)
        {
            DiscordEmbedBuilder embed = BaseEmbed();
            embed.Description = $"Todavía nadie tiene **{AnilistMediaFormatter.DisplayTitle(media)}** en su lista.";
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            return;
        }

        bool hayAmbasListas = view.ConScore.Count > 0 && view.SinScore.Count > 0;
        const int lineasPorPagina = 30;

        List<(string Id, string Label, IReadOnlyList<DiscordEmbed> Paginas)> tabs = [];

        if (view.ConScore.Count > 0)
        {
            string descripcion = string.Join('\n', view.ConScore);
            IReadOnlyList<DiscordEmbed> paginas = DiscordInteractivity.GenerarPaginasPorLineas(descripcion, lineasPorPagina, BaseEmbed());
            tabs.Add(("con-score", "Con score", paginas));
        }

        if (view.SinScore.Count > 0)
        {
            // Con las dos listas, el botón de la pestaña ya indica "Sin scores asignados"; evitar repetir la etiqueta.
            string encabezado = hayAmbasListas ? string.Empty : $"{Formatter.Bold("Sin scores asignados:")}\n";
            string descripcion = encabezado + string.Join('\n', view.SinScore);
            IReadOnlyList<DiscordEmbed> paginas = DiscordInteractivity.GenerarPaginasPorLineas(descripcion, lineasPorPagina, BaseEmbed());
            tabs.Add(("sin-score", "Sin scores asignados", paginas));
        }

        await DiscordInteractivity.SwitchTabsPaginadoAsync(ctx, tabs);
    }
    
    [Command("usuarioanilist")]
    [Description("Busca un usuario de AniList para saber si se encuentra en el servidor")]
    public async Task Busqueda(SlashCommandContext ctx, [Parameter("Nombre")][Description("Nombre de usuario en AniList")] string buscar)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        AnilistUser? usuarioAnilist;
        try
        {
            usuarioAnilist = await anilistClient.SearchUserAsync(buscar);
        }
        catch (AnilistServerErrorException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AnilistErrorEmbed.NoDisponible()));
            return;
        }

        if (usuarioAnilist is not null)
        {
            UsuarioAnilist? vinculado = (await usuariosRepository.GetVinculados()).Find(x => x.AnilistURL == usuarioAnilist.SiteUrl);
            if (vinculado is not null && ctx.Guild!.Members.TryGetValue((ulong)vinculado.UserId, out DiscordMember? miembro))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Usuario encontrado",
                    Description = $"El usuario de AniList `{buscar}` se encuentra en el servidor y es `{miembro.DisplayName}` ({miembro.Mention})",
                    Color = DiscordColor.Green
                }.WithThumbnail(miembro.GuildAvatarUrl ?? miembro.AvatarUrl)));

                return;
            }
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Usuario no encontrado",
            Description = $"El usuario `{buscar}` no se encuentra en el servidor",
            Color = DiscordColor.Red
        }));
    }
}

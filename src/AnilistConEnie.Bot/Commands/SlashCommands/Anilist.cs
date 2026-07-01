using System.ComponentModel;
using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Exceptions;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Services;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[TestCommand]
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

        DiscordEmbed? conScoreEmbed = null;
        if (view.ConScore.Count > 0)
        {
            DiscordEmbedBuilder embed = BaseEmbed();
            string promedio = $"{Formatter.Bold("Promedio:")} {Math.Round(view.Average100 ?? 0, 2)}/100\n\n";
            embed.Description = TruncarDescripcion(promedio + string.Join('\n', view.ConScore));
            conScoreEmbed = embed.Build();
        }

        DiscordEmbed? sinScoreEmbed = null;
        if (view.SinScore.Count > 0)
        {
            DiscordEmbedBuilder embed = BaseEmbed();
            // Con las dos listas, el botón de la pestaña ya indica "Sin scores asignados"; evitar repetir la etiqueta.
            string encabezado = hayAmbasListas ? string.Empty : $"{Formatter.Bold("Sin scores asignados:")}\n";
            embed.Description = TruncarDescripcion(encabezado + string.Join('\n', view.SinScore));
            sinScoreEmbed = embed.Build();
        }

        if (hayAmbasListas)
        {
            Dictionary<string, DiscordEmbed> tabs = new()
            {
                ["Con score"] = conScoreEmbed!,
                ["Sin scores asignados"] = sinScoreEmbed!
            };
            await DiscordInteractivity.SwitchTabsAsync(ctx, tabs);
            return;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(conScoreEmbed ?? sinScoreEmbed!));
    }

    private const int EmbedDescriptionLimit = 4096;

    private static string TruncarDescripcion(string texto)
    {
        if (texto.Length <= EmbedDescriptionLimit) return texto;

        string cortado = texto[..EmbedDescriptionLimit];
        int ultimoSalto = cortado.LastIndexOf('\n');
        if (ultimoSalto > 0) cortado = cortado[..ultimoSalto];

        return cortado + "\n\n*(lista truncada, hay más miembros de los que entran acá)*";
    }
    
    [Command("usuarioanilist")]
    [Description("Busca un usuario de AniList para saber si se encuentra en el servidor")]
    public async Task Busqueda(CommandContext ctx, [Parameter("Nombre")][Description("Nombre de usuario en AniList")] string buscar)
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
    
    [Command("modvincularanilist")]
    [Description("Registra un AniList en el servidor para otro usuario (Staff)")]
    public async Task SetAnilistMod(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("Usuario de Discord para asignarle el AniList")] DiscordUser user,
        [Parameter("Perfil")] [Description("URL o nombre de usuario del perfil de AniList")] string perfil)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        if (ctx.Member is null || ctx.Member.Roles.All(r => r.Id != config.Roles.KamiSama))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Sin permiso",
                Description = "Solo un Kami Sama puede usar este comando."
            }));
            return;
        }

        string nombre = AnilistProfileUrl.ExtractUserName(perfil);

        AnilistUser? anilistUser;
        try
        {
            anilistUser = await anilistClient.SearchUserAsync(nombre);
        }
        catch (AnilistServerErrorException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AnilistErrorEmbed.NoDisponible()));
            return;
        }

        if (anilistUser is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Error",
                Description = $"No se encontró el usuario de AniList `{nombre}`"
            }));
            return;
        }

        DiscordMember miembro;
        try
        {
            miembro = await ctx.Guild!.GetMemberAsync(user.Id);
        }
        catch (NotFoundException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Error",
                Description = $"{user.Mention} no se encuentra en el servidor"
            }));
            return;
        }

        await anilistService.TerminarVinculacion(ctx.Client, user, miembro, ctx.Guild!, anilistUser);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Title = "Perfil guardado",
            Description = $"Se vinculó el perfil de AniList de {miembro.Mention} correctamente"
        }));
    }
}

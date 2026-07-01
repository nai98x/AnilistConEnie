using System.ComponentModel;
using System.Globalization;
using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Bot.Commands.AutoComplete;
using AnilistConEnie.Bot.Commands.Slash.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Exceptions;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using AnilistConEnie.Bot.Extensions;

namespace AnilistConEnie.Bot.Commands.Slash;

[Command("challenges")]
[Description("Comandos de los challenges del servidor")]
//[TestCommand]
public class Challenges(
    IChallengesRepository challengesRepository,
    IXpUsuariosRepository xpUsuariosRepository,
    IUsuariosRepository usuariosRepository,
    XpState xpState,
    AnilistService anilistService,
    DiscordBotService discordBotService,
    BotConfiguration config)
{
    [Command("set")]
    [Description("Agrega o modifica un challenge (Staff)")]
    [RequirePermissions(DiscordPermission.ManageGuild)]
    public async Task Set(
        SlashCommandContext ctx,
        [Parameter("Nombre")] [Description("Nombre del challenge")] string nombre,
        [Parameter("Link")] [Description("Link del challenge")] string link,
        [Parameter("Disponible")] [Description("Si el challenge se puede realizar")] bool disponible,
        [Parameter("Vencimiento")] [Description("Vencimiento del challenge (dd/MM/yyyy)")] string? vencimiento = null)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        if (ctx.Member is null || !ctx.Member.Permissions.HasPermission(DiscordPermission.ManageGuild))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Sin permiso",
                Description = "Necesitas el permiso de `Gestionar servidor` para usar este comando.",
                Color = DiscordColor.Red
            }));
            return;
        }

        string dispStr = disponible ? "Disponible" : "No disponible";
        DateTime? fechaVencimiento = null;

        if (!string.IsNullOrEmpty(vencimiento))
        {
            if (!DateTime.TryParseExact(vencimiento, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fchVnc))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = $"Fecha `{vencimiento}` invalida (debe ser dd/MM/yyyy)",
                    Color = DiscordColor.Red
                }));
                return;
            }

            fechaVencimiento = new DateTime(fchVnc.Year, fchVnc.Month, fchVnc.Day, 5, 0, 0, DateTimeKind.Utc);
        }

        await challengesRepository.Upsert(nombre, link, disponible, fechaVencimiento);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Nuevo challenge creado",
            Description = $"[{nombre}]({link}) ({dispStr})",
            Color = DiscordColor.Green
        }));
    }

    [Command("lista")]
    [Description("Permite ver los challenges del servidor")]
    public async Task Lista(SlashCommandContext ctx)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<Challenge> challenges = await challengesRepository.GetLista();
        string desc = challenges.Count == 0
            ? "(Sin challenges disponibles)"
            : ChallengeFormatter.ChunkByDisponibilidad(challenges);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Challenges del servidor",
            Description = desc,
            Color = DiscordEmojiHelper.GetColor()
        }));
    }

    [Command("ver")]
    [Description("Permite ver los usuarios que han completado un challenge")]
    public async Task Ver(
        SlashCommandContext ctx,
        [Parameter("Challenge")] [Description("Challenge a elegir")] [SlashAutoCompleteProvider<ChallengesAutoCompleteProvider>] string challenge)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        List<Challenge> challenges = await challengesRepository.GetLista();
        Challenge? challengeData = challenges.Find(x => x.Nombre == challenge);
        if (challengeData is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = $"No existe el challenge `{challenge}`",
                Color = DiscordColor.Red
            }));
            return;
        }

        DiscordEmoji umaPoints = await DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.UmaPoints.Get(discordBotService.Debug));

        List<ChallengeCompletado> completaron = await challengesRepository.GetListaUsuariosCompletaron(challenge);
        string descTerminados = "(Ningún usuario ha completado este challenge)";
        if (completaron.Count > 0)
        {
            descTerminados = string.Empty;
            foreach (ChallengeCompletado x in completaron)
            {
                if (ctx.Guild!.Members.TryGetValue((ulong)x.UserId, out DiscordMember? member))
                    descTerminados += $"- {member.DisplayName} - **XP:** {x.Xp} {umaPoints}\n";
            }
        }

        DiscordEmbedBuilder embedTerminados = new DiscordEmbedBuilder()
            .WithTitle($"Usuarios que completaron el {challenge}")
            .WithDescription(descTerminados)
            .WithColor(DiscordColor.Green);

        List<UsuarioAnilist> participantes;
        try
        {
            participantes = await anilistService.PostsFromChallengeAsync(challengeData);
        }
        catch (AnilistServerErrorException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AnilistErrorEmbed.NoDisponible()));
            return;
        }

        List<UsuarioAnilist> pendientes = participantes.Where(x => completaron.All(y => y.UserId != x.UserId)).ToList();

        string descPendientes = "(Ningún usuario tiene pendiente este challenge)";
        if (pendientes.Count > 0)
        {
            descPendientes = string.Empty;
            foreach (UsuarioAnilist x in pendientes)
            {
                if (ctx.Guild!.Members.TryGetValue((ulong)x.UserId, out DiscordMember? member))
                    descPendientes += $"- {member.DisplayName}\n";
            }
        }

        DiscordEmbedBuilder embedPendientes = new DiscordEmbedBuilder().WithDescription(descPendientes);
        if (challengeData.Disponible)
        {
            embedPendientes.WithTitle($"Usuarios que están haciendo el {challenge}");
            embedPendientes.WithColor(DiscordColor.Yellow);
        }
        else
        {
            embedPendientes.WithTitle($"Usuarios que no pudieron realizar el {challenge}");
            embedPendientes.WithColor(DiscordColor.Red);
        }

        Dictionary<string, DiscordEmbed> tabs = new()
        {
            { "Completados", embedTerminados.Build() },
            { "Pendientes", embedPendientes.Build() }
        };

        await DiscordInteractivity.SwitchTabsAsync(ctx, tabs);
    }

    [Command("usuario")]
    [Description("Ve los challenges de un usuario")]
    public async Task Usuario(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("Usuario a consultar challenges")] DiscordMember usuario)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Procesando...",
            Description = $"Consultando los challenges de {usuario.Mention}. Esto puede tardar unos segundos.",
            Color = DiscordColor.Blurple,
            Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = "La demora se debe a los ratelimits de la API de AniList."
            }
        }));

        try
        {
            UsuarioAnilist? userAnilist = await usuariosRepository.GetPerfil(usuario.Id);
            if (userAnilist is null || !AnilistProfileUrl.TryGetUserId(userAnilist.AnilistURL, out int anilistUserId))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = $"{usuario.Mention} no tiene un AniList vinculado",
                    Color = DiscordColor.Red
                }));
                return;
            }

            DiscordEmoji umaPoints = await DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.UmaPoints.Get(discordBotService.Debug));

            List<UsuarioChallenge> challengesUsuario = await challengesRepository.GetChallengesUsuario(usuario.Id);
            List<Challenge> todos = await challengesRepository.GetLista();
            List<Challenge> noCompletados = todos.Where(x => challengesUsuario.All(y => y.Challenge.Nombre != x.Nombre)).ToList();
            List<Challenge> enCurso = await anilistService.ChallengesPostsFromMemberAsync(anilistUserId, noCompletados);
            List<Challenge> sinPost = noCompletados.Where(x => enCurso.All(y => y.Nombre != x.Nombre)).ToList();
            List<Challenge> sinPostDisponibles = sinPost.Where(x => x.Disponible).ToList();
            List<Challenge> sinPostNoDisponibles = sinPost.Where(x => !x.Disponible).ToList();

            string descCompletados = "(Ningún challenge completado)";
            if (challengesUsuario.Count > 0)
            {
                descCompletados = string.Empty;
                int xpTotal = 0;
                foreach (UsuarioChallenge x in challengesUsuario)
                {
                    descCompletados += $"[{x.Challenge.Nombre}]({x.Challenge.Link}) - {x.Xp}\n";
                    xpTotal += x.Xp;
                }

                descCompletados += $"\nTotal de XP obtenida: **{xpTotal}** {umaPoints}";
            }

            DiscordEmbed embedCompletados = new DiscordEmbedBuilder()
                .WithTitle($"Challenges completados por {usuario.DisplayName}")
                .WithDescription(descCompletados)
                .WithColor(DiscordEmojiHelper.GetColor())
                .Build();

            string descEnCurso = enCurso.Count > 0
                ? ChallengeFormatter.ChunkIfDisponibles(enCurso, true)
                : "(Ningún challenge en curso)";

            DiscordEmbed embedEnCurso = new DiscordEmbedBuilder()
                .WithTitle($"Challenges en curso por {usuario.DisplayName}")
                .WithDescription(descEnCurso)
                .WithColor(DiscordEmojiHelper.GetColor())
                .Build();

            string descSinPost = "(Ningún challenge pendiente de hacer el post)";
            if (sinPostDisponibles.Count > 0)
            {
                descSinPost = string.Empty;
                foreach (Challenge x in sinPostDisponibles)
                    descSinPost += $"[{x.Nombre}]({x.Link})\n";
            }

            DiscordEmbed embedSinPost = new DiscordEmbedBuilder()
                .WithTitle($"Challenges sin posts publicados de {usuario.DisplayName}")
                .WithDescription(descSinPost)
                .WithColor(DiscordEmojiHelper.GetColor())
                .Build();

            string descNoDisponibles = "**Post realizado:**\n";
            descNoDisponibles += ChallengeFormatter.ChunkIfDisponibles(enCurso, false);
            descNoDisponibles += "**Post no creado:**\n";
            if (sinPostNoDisponibles.Count > 0)
            {
                foreach (Challenge x in sinPostNoDisponibles)
                    descNoDisponibles += $"[{x.Nombre}]({x.Link})\n";
            }
            else
            {
                descNoDisponibles += "(Ningún challenge no disponible)\n";
            }

            DiscordEmbed embedNoDisponibles = new DiscordEmbedBuilder()
                .WithTitle($"Challenges no disponibles de {usuario.DisplayName}")
                .WithDescription(descNoDisponibles)
                .WithColor(DiscordColor.Red)
                .Build();

            Dictionary<string, DiscordEmbed> tabs = new()
            {
                { "Completados", embedCompletados },
                { "En curso", embedEnCurso },
                { "No inscritos", embedSinPost },
                { "No disponibles", embedNoDisponibles }
            };

            await DiscordInteractivity.SwitchTabsAsync(ctx, tabs);
        }
        catch (AnilistServerErrorException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AnilistErrorEmbed.NoDisponible()));
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle($"Error obteniendo challenges de {usuario.DisplayName}")
                .WithDescription(Formatter.BlockCode(ex.Message))
                .WithColor(DiscordColor.Red)));
        }
    }

    [Command("completar")]
    [Description("Agrega un challenge a un usuario (Staff)")]
    public async Task Completar(
        SlashCommandContext ctx,
        [Parameter("Challenge")] [Description("Challenge a elegir")] [SlashAutoCompleteProvider<ChallengesAutoCompleteProvider>] string challenge,
        [Parameter("Usuario")] [Description("Usuario que completo el challenge")] DiscordUser usuario,
        [Parameter("Xp")] [Description("XP recibida por completarlo")] double xp,
        [Parameter("Imagen")] [Description("URL de la imagen de la placa del challenge")] string imagen1,
        [Parameter("Imagen2")] [Description("URL de la imagen de la placa del challenge")] string? imagen2 = null,
        [Parameter("Imagen3")] [Description("URL de la imagen de la placa del challenge")] string? imagen3 = null)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferResponseAsync();

        if (ctx.Member is null || ctx.Member.Roles.All(r => r.Id != config.Roles.Colaborador))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Sin permiso",
                Description = "Solo el staff puede usar este comando.",
                Color = DiscordColor.Red
            }));
            return;
        }

        UserXp memberXp = xpState.GetUserXp(usuario.Id);
        long totalXp = memberXp.Total + (long)xp;
        long totalChallenges = memberXp.Challenges + (long)xp;
        xpState.UpdateUserXp(usuario.Id, totalXp, TipoXp.Total);
        xpState.UpdateUserXp(usuario.Id, totalChallenges, TipoXp.Challenges);

        await challengesRepository.SetUsuarioChallenge(challenge, usuario.Id, (int)xp, ctx.Interaction.CreationTimestamp);
        await xpUsuariosRepository.AddRemove(usuario.Id, new UserXpDelta { Total = (int)xp, Challenges = (int)xp });

        DiscordEmoji umaPoints = await DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.UmaPoints.Get(discordBotService.Debug));

        DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder()
            .WithContent(usuario.Mention)
            .AddMention(new UserMention(usuario.Id))
            .AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Challenges completado",
                Description = $"¡Felicitaciones {usuario.Mention}! Completaste el `{challenge}` y ganaste **{xp} {umaPoints} de xp**.",
                Color = DiscordColor.Green,
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = "https://media.discordapp.net/attachments/862568630365323264/990747470508204032/unknown.png"
                }
            })
            .AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Blurple,
                ImageUrl = imagen1
            });

        if (imagen2 != null)
            builder.AddEmbed(new DiscordEmbedBuilder { Color = DiscordColor.Blurple, ImageUrl = imagen2 });

        if (imagen3 != null)
            builder.AddEmbed(new DiscordEmbedBuilder { Color = DiscordColor.Blurple, ImageUrl = imagen3 });

        await ctx.FollowupAsync(builder);
    }
}

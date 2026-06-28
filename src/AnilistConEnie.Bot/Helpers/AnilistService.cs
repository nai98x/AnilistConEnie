using System.Text;
using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Exceptions;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Model.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Helpers;

public class AnilistService(XpState xpState, ChallengePostsState challengePostsState, BotConfiguration config, LimpiezaMiembrosSettings limpiezaSettings, RangoRoles rangoRoles, DiscordLogService logService, AnilistServerScoreService scoreService, IFirebaseRepository firebaseRepository, IUsuariosRepository usuariosRepository, IAnilistBaneadosRepository anilistBaneadosRepository, IAnilistApprovalRepository anilistApprovalRepository, IAnilistClient anilistClient)
{
    public async Task TerminarVinculacion(DiscordClient client, DiscordUser user, DiscordMember member, DiscordGuild guild, AnilistUser anilistUser)
    {
        DiscordEmbedBuilder bienvenidaEmbed = new()
        {
            Title = $"¡Bienvenido {member.DisplayName}!",
            Description = $"Hola {member.Mention}, ¡Bienvenido a **Añilist**! Eres nuestro **miembro nº {guild.MemberCount}**",
            ImageUrl = $"https://images-ext-2.discordapp.net/external/S1VMfYfgS0oqoMukCNxKw5HsZxZQTEvWkpG-4Q3qVyA/https/cdn-longterm.mee6.xyz/plugins/welcome/images/{config.GuildId}/20b91584d1b680f1905f5b2f5295a44907bd17e876c56ab10de43f1cd406d1db.gif",
            Color = DiscordEmojiHelper.GetColor()
        };

        DiscordEmbedBuilder newProfileEmbed = new()
        {
            Color = DiscordColor.Green,
            Title = "Nuevo perfil vinculado",
            Description = $"AniList de {user.Mention}",
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = anilistUser.AvatarMedium ?? string.Empty
            },
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                Url = anilistUser.SiteUrl,
                Name = anilistUser.Name,
                IconUrl = user.AvatarUrl
            }
        };

        if (!string.IsNullOrEmpty(anilistUser.BannerImage))
        {
            newProfileEmbed.WithImageUrl(anilistUser.BannerImage);
        }

        DiscordLinkButtonComponent profile = new($"{anilistUser.SiteUrl}", "Perfil", false, new DiscordComponentEmoji("👤"));
        DiscordLinkButtonComponent animeList = new($"{anilistUser.SiteUrl}/animelist", "Lista de anime", false, new DiscordComponentEmoji("📺"));
        DiscordLinkButtonComponent mangaList = new($"{anilistUser.SiteUrl}/mangalist", "Lista de manga", false, new DiscordComponentEmoji("📖"));

        DiscordChannel perfiles = guild.Channels[config.Channels.Perfiles];
        DiscordMessage? mensaje = null;
        UsuarioAnilist? usrPreexistente = await usuariosRepository.GetPerfil(member.Id);
        
        try
        {
            if (usrPreexistente is not null)
            {
                mensaje = await perfiles.GetMessageAsync((ulong)usrPreexistente.MessageId);
                await mensaje.ModifyAsync($"**Perfil de {member.Mention}**\n\n{anilistUser.SiteUrl}");
            }
        }
        catch (NotFoundException ex) { await logService.LogException(guild, ex, "TerminarVinculacion - perfil preexistente"); }
        
        if (usrPreexistente is null)
            mensaje = await perfiles.SendMessageAsync($"**Perfil de {member.Mention}**\n\n{anilistUser.SiteUrl}");
        
        await usuariosRepository.Upsert(member.Id, anilistUser.SiteUrl, (long)mensaje!.Id);
        await firebaseRepository.SetAnilistYumiko(anilistUser.Id, member.Id);

        UserXp prevXp = xpState.GetUserXp(user.Id);

        DiscordMessageBuilder msgBuilder = new DiscordMessageBuilder()
            .WithContent(user.Mention)
            .AddEmbed(bienvenidaEmbed.Build())
            .AddEmbed(newProfileEmbed.Build())
            .AddActionRowComponent(profile, animeList, mangaList)
            .WithAllowedMention(new UserMention(user.Id));

        await guild.Channels[config.Channels.General].SendMessageAsync(msgBuilder);

        try
        {
            DiscordRole miembroRole = guild.Roles[config.Roles.Miembro];
            DiscordRole desvinculadoRole = guild.Roles[config.Roles.NoVinculado];
            await member.GrantRoleAsync(miembroRole);
            await member.RevokeRoleAsync(desvinculadoRole);

            if (prevXp.Total > 0)
            {
                DiscordRole role = rangoRoles.GetRoleByXp(guild, prevXp.Total);
                if (!member.Roles.Any(x => x.Id == role.Id))
                {
                    await member.GrantRoleAsync(role);
                }
            }
        }
        catch (Exception ex)
        {
            await logService.GrabarLogGeneralError(guild, $"Error agregando rol miembro al vincular AniList para el usuario {user.Id}\n\n{ex.Message}:\n\n{ex.StackTrace}");
        }
    }

    public async Task<string> GetServerScoresAsync(DiscordGuild guild, AnilistMedia media, bool includeUsersWithoutScore)
    {
        Dictionary<ulong, DiscordMember> miembros = [];
        await foreach (DiscordMember miembro in guild.GetAllMembersAsync())
            miembros[miembro.Id] = miembro;

        Dictionary<int, string> porAnilistId = [];
        foreach (UsuarioAnilist usuario in await usuariosRepository.GetVinculados())
        {
            if (!miembros.TryGetValue((ulong)usuario.UserId, out DiscordMember? miembro)
                || !rangoRoles.RangoAPartirDe(guild, miembro, RangoEnum.Miembro, true))
                continue;

            if (AnilistProfileUrl.TryGetUserId(usuario.AnilistURL, out int anilistId))
                porAnilistId[anilistId] = miembro.DisplayName;
        }

        ServerMediaScores result = await scoreService.AggregateAsync(media, porAnilistId, includeUsersWithoutScore);
        return FormatScores(result, media);
    }
    
    public async Task VincularAniList(DiscordInteraction interaction, DiscordClient client)
    {
        InteractivityExtension interactivity = client.ServiceProvider.GetRequiredService<InteractivityExtension>();
        string modalId = $"modal-{interaction.Id}";

        DiscordModalBuilder modal = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Vincular AniList")
            .AddTextInput(new DiscordTextInputComponent("AniListToken", placeholder: "Pegar código aquí"), "Código");

        await interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        var modalResult = await interactivity.WaitForModalAsync(modalId, TimeSpan.FromMinutes(5));
        if (modalResult.TimedOut) return;

        DiscordInteraction modalInteraction = modalResult.Result.Interaction;
        string accessToken = ((TextInputModalSubmission)modalResult.Result.Values["AniListToken"]).Value;

        await modalInteraction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

        AnilistViewer? viewer;
        try
        {
            viewer = await anilistClient.GetViewerAsync(accessToken);
        }
        catch (AnilistApiException ex)
        {
            await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = ex.Message,
                Color = DiscordColor.Red
            }));
            return;
        }

        if (viewer is null)
        {
            await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = "Error desconocido",
                Color = DiscordColor.Red
            }));
            return;
        }

        DiscordMember member = await interaction.Guild!.GetMemberAsync(interaction.User.Id, true);

        if (await anilistBaneadosRepository.Existe(viewer.Id))
        {
            await interaction.Guild.BanMemberAsync(interaction.User.Id, TimeSpan.Zero, "Usuario baneado por AniList baneado");
            return;
        }

        AnilistUser anilistUser = new()
        {
            Id = viewer.Id,
            Name = viewer.Name,
            SiteUrl = viewer.SiteUrl,
            AvatarMedium = viewer.AvatarMedium,
            BannerImage = viewer.BannerImage
        };

        IReadOnlyList<MotivoAprobacionDetalle> motivosAprobacion = AnilistApprovalPolicy.Evaluar(
            await usuariosRepository.GetVinculados(),
            interaction.User.Id, viewer.Id,
            interaction.User.CreationTimestamp, viewer.CreatedAt,
            limpiezaSettings.CuentaNuevaMeses, DateTimeOffset.Now);

        if (motivosAprobacion.Count > 0)
        {
            List<string> lineas = [];
            foreach (MotivoAprobacionDetalle r in motivosAprobacion)
            {
                if (r.Motivo == MotivoAprobacion.PosibleMulticuenta)
                {
                    DiscordMember? otroMiembro = await TryGetMemberAsync(interaction.Guild, (ulong)r.OtroMiembroId!.Value);
                    string otroNombre = otroMiembro?.DisplayName ?? "desconocido";
                    lineas.Add($"su AniList ya está vinculado a otro miembro activo ({Formatter.InlineCode(otroNombre)} | Id: {Formatter.InlineCode(((ulong)r.OtroMiembroId.Value).ToString())}), posible multicuenta");
                }
                else
                    lineas.Add("su cuenta de Discord o AniList es muy reciente");
            }
            string motivos = string.Concat(lineas.Select(m => $"- {m}\n"));

            DiscordButtonComponent aprobar = new(DiscordButtonStyle.Success, $"sync-true-{interaction.User.Id}", "Aprobar");
            DiscordButtonComponent denegar = new(DiscordButtonStyle.Danger, $"sync-false-{interaction.User.Id}", "Denegar");

            DiscordMessageBuilder msgBuilderApproval = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Yellow,
                    Title = "Vinculacion de AniList pendiente de aprobación",
                    Description =
                        $"El usuario {interaction.User.Mention} | Nombre: {Formatter.InlineCode(member.DisplayName)} | Id: {Formatter.InlineCode(interaction.User.Id.ToString())} ({Formatter.MaskedUrl("Link de su AniList", new Uri(viewer.SiteUrl))}) ha vinculado su cuenta de AniList pero debe ser aprobada manualmente por el staff por los siguientes motivos:\n{motivos}\n" +
                        $"- Discord: {interaction.User.CreationTimestamp:dd/MM/yyyy}\n" +
                        $"- AniList: {viewer.CreatedAt.ToLocalTime():dd/MM/yyyy}",
                })
                .AddActionRowComponent(aprobar, denegar);

            UserApprovalAnilist userApproval = new()
            {
                IdDiscord = (long)interaction.User.Id,
                IdAnilist = viewer.Id,
                Name = viewer.Name,
                SiteUrl = viewer.SiteUrl,
                Avatar = viewer.AvatarMedium ?? string.Empty,
                Banner = viewer.BannerImage ?? string.Empty
            };

            await anilistApprovalRepository.Upsert(userApproval);
            await interaction.Guild.Channels[config.Channels.Moderacion].SendMessageAsync(msgBuilderApproval);

            await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Atencion",
                Description = "La cuenta debe ser vinculada manualmente por el staff.",
                Color = DiscordColor.Yellow
            }));

            return;
        }

        await TerminarVinculacion(client, interaction.User, member, interaction.Guild, anilistUser);
        await modalInteraction.DeleteOriginalResponseAsync();
    }

    /// <summary>
    /// De la lista de challenges indicada, devuelve aquellos en cuyo post de AniList el usuario
    /// (<paramref name="anilistUserId"/>) dejó un reply, es decir, los que está realizando.
    /// </summary>
    public async Task<List<Challenge>> ChallengesPostsFromMemberAsync(int anilistUserId, IReadOnlyList<Challenge> challenges)
    {
        List<Challenge> ret = [];
        foreach (Challenge challenge in challenges)
        {
            if (!AnilistActivityUrl.TryGetActivityId(challenge.Link, out int activityId)) continue;

            IReadOnlyList<int> replyUserIds = await GetActivityReplyUserIdsCachedAsync(activityId);
            if (replyUserIds.Contains(anilistUserId))
                ret.Add(challenge);
        }

        return ret;
    }

    /// <summary>
    /// Usuarios del servidor (con AniList vinculado) que dejaron un reply en el post del challenge,
    /// es decir, los que están participando en él.
    /// </summary>
    public async Task<List<UsuarioAnilist>> PostsFromChallengeAsync(Challenge challenge)
    {
        List<UsuarioAnilist> ret = [];
        if (!AnilistActivityUrl.TryGetActivityId(challenge.Link, out int activityId)) return ret;

        List<UsuarioAnilist> vinculados = await usuariosRepository.GetVinculados();
        IReadOnlyList<int> replyUserIds = await GetActivityReplyUserIdsCachedAsync(activityId);
        foreach (int replyUserId in replyUserIds)
        {
            UsuarioAnilist? usuario = vinculados.Find(x =>
                AnilistProfileUrl.TryGetUserId(x.AnilistURL, out int id) && id == replyUserId);

            if (usuario is not null && !ret.Contains(usuario))
                ret.Add(usuario);
        }

        return ret;
    }

    /// <summary>
    /// Devuelve los ids de usuario que respondieron al post, usando la cache compartida
    /// (<see cref="ChallengePostsState"/>) y consultando a AniList solo si no hay un valor vigente.
    /// </summary>
    private async Task<IReadOnlyList<int>> GetActivityReplyUserIdsCachedAsync(int activityId)
    {
        if (challengePostsState.TryGet(activityId, out IReadOnlyList<int> cached))
            return cached;

        IReadOnlyList<int> replyUserIds = await anilistClient.GetActivityReplyUserIdsAsync(activityId);
        challengePostsState.Set(activityId, replyUserIds);
        return replyUserIds;
    }

    private static async Task<DiscordMember?> TryGetMemberAsync(DiscordGuild guild, ulong userId)
    {
        try
        {
            return await guild.GetMemberAsync(userId);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static string FormatScores(ServerMediaScores result, AnilistMedia media)
    {
        if (result.IsEmpty) return string.Empty;

        List<string> conScore = [];
        foreach (MemberMediaScore m in result.Scored)
        {
            string link = Formatter.MaskedUrl(m.DisplayName, new Uri(m.Entry.UserSiteUrl));
            string score = AnilistMediaFormatter.UserScore(m.Entry);
            conScore.Add(m.Entry.Status == "COMPLETED"
                ? $"{link} - {score}\n"
                : $"{link} - {score} {Formatter.InlineCode($"{m.Entry.Status} - Progreso: {FormatProgreso(media, m.Entry.Progress)}")}\n");
        }

        List<string> sinScore = [];
        foreach (MemberMediaScore m in result.Unscored)
        {
            string link = Formatter.MaskedUrl(m.DisplayName, new Uri(m.Entry.UserSiteUrl));
            sinScore.Add($"{link} - {Formatter.InlineCode($"{m.Entry.Status} - Progreso: {FormatProgreso(media, m.Entry.Progress)}")}\n");
        }

        StringBuilder sb = new();

        if (conScore.Count > 0)
        {
            sb.Append($"{Formatter.Bold("Promedio:")} {Math.Round(result.Average100 ?? 0, 2)}/100\n\n");

            conScore.Sort();
            sb.Append(string.Concat(conScore));

            if (sinScore.Count > 0)
            {
                sinScore.Sort();
                sb.Append($"\n{Formatter.Bold("Sin scores asignados:")}\n");
                sb.Append(string.Concat(sinScore));
            }
        }
        else
        {
            sinScore.Sort();
            sb.Append($"{Formatter.Bold("Sin scores asignados:")}\n");
            sb.Append(string.Concat(sinScore));
        }

        return sb.ToString();
    }

    private static string FormatProgreso(AnilistMedia media, int? progress)
    {
        string actual = (progress ?? 0).ToString();
        int? total = media.Episodes ?? media.Chapters;
        return total is { } t ? $"{actual}/{t}" : actual;
    }
}
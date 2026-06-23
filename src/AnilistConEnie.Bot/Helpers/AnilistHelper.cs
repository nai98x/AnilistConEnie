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

public class AnilistHelper(XpState xpState, AnilistUsersState anilistUsersState, BotConfiguration config, DiscordHelper discordHelper, AnilistServerScoreService scoreService, IUsuariosAnilistRepository usuariosAnilistRepository, IAnilistClient anilistClient)
{
    public async Task TerminarVinculacion(DiscordClient client, DiscordUser user, DiscordMember member, DiscordGuild guild, UserApprovalAnilist userApproval)
    {
        DiscordEmbedBuilder bienvenidaEmbed = new()
        {
            Title = $"¡Bienvenido {member.DisplayName}!",
            Description = $"Hola {member.Mention}, ¡Bienvenido a **Añilist**! Eres nuestro **miembro nº {guild.MemberCount}**",
            ImageUrl = $"https://images-ext-2.discordapp.net/external/S1VMfYfgS0oqoMukCNxKw5HsZxZQTEvWkpG-4Q3qVyA/https/cdn-longterm.mee6.xyz/plugins/welcome/images/{config.GuildId}/20b91584d1b680f1905f5b2f5295a44907bd17e876c56ab10de43f1cd406d1db.gif",
            Color = DiscordHelper.GetColor()
        };

        DiscordEmbedBuilder newProfileEmbed = new()
        {
            Color = DiscordColor.Green,
            Title = "Nuevo perfil vinculado",
            Description = $"AniList de {user.Mention}",
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = userApproval.Avatar
            },
            Author = new DiscordEmbedBuilder.EmbedAuthor
            {
                Url = userApproval.SiteUrl,
                Name = userApproval.Name,
                IconUrl = user.AvatarUrl
            }
        };

        if (!string.IsNullOrEmpty(userApproval.Banner))
        {
            newProfileEmbed.WithImageUrl(userApproval.Banner);
        }

        DiscordLinkButtonComponent profile = new($"{userApproval.SiteUrl}", "Perfil", false, new DiscordComponentEmoji("👤"));
        DiscordLinkButtonComponent animeList = new($"{userApproval.SiteUrl}/animelist", "Lista de anime", false, new DiscordComponentEmoji("📺"));
        DiscordLinkButtonComponent mangaList = new($"{userApproval.SiteUrl}/mangalist", "Lista de manga", false, new DiscordComponentEmoji("📖"));

        DiscordChannel perfiles = guild.Channels[config.Channels.Perfiles];
        DiscordMessage? mensaje = null;
        UsuarioAnilist? usrPreexistente = await usuariosAnilistRepository.GetPerfil(member.Id);
        
        try
        {
            if (usrPreexistente is not null)
            {
                mensaje = await perfiles.GetMessageAsync((ulong)usrPreexistente.MessageId);
                await mensaje.ModifyAsync($"**Perfil de {member.Mention}**\n\n{userApproval.SiteUrl}");
            }
        }
        catch (NotFoundException) { /* Ignored */ }
        
        if (usrPreexistente is null)
            mensaje = await perfiles.SendMessageAsync($"**Perfil de {member.Mention}**\n\n{userApproval.SiteUrl}");
        
        await usuariosAnilistRepository.SetAnilist(member.Id, userApproval.SiteUrl, (long)mensaje!.Id);
        await usuariosAnilistRepository.SetAnilistYumiko(userApproval.IdAnilist, member.Id);

        List<UsuarioAnilist> users = anilistUsersState.Usuarios;
        if (!users.Where(u => (ulong)u.UserId == user.Id).Any())
        {
            List<UsuarioAnilist> newList = await usuariosAnilistRepository.GetListaUsuarios();
            UsuarioAnilist newUser = newList.First(u => (ulong)u.UserId == user.Id);
            users.Add(newUser);
            anilistUsersState.SetUsuarios(users);
        }

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
                DiscordRole role = discordHelper.GetRoleByXp(guild, prevXp.Total);
                if (!member.Roles.Any(x => x.Id == role.Id))
                {
                    await member.GrantRoleAsync(role);
                }
            }
        }
        catch (Exception ex)
        {
            await discordHelper.GrabarLogGeneralError(guild, $"Error agregando rol miembro al vincular AniList para el usuario {user.Id}\n\n{ex.Message}:\n\n{ex.StackTrace}");
        }
    }

    public async Task<string> GetServerScoresAsync(DiscordGuild guild, AnilistMedia media, bool includeUsersWithoutScore)
    {
        Dictionary<ulong, DiscordMember> miembros = [];
        await foreach (DiscordMember miembro in guild.GetAllMembersAsync())
            miembros[miembro.Id] = miembro;

        Dictionary<int, string> porAnilistId = [];
        foreach (UsuarioAnilist usuario in anilistUsersState.Usuarios)
        {
            if (!miembros.TryGetValue((ulong)usuario.UserId, out DiscordMember? miembro)
                || !discordHelper.RangoAPartirDe(guild, miembro, RangoEnum.Miembro, true))
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

        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id, true);

        if (anilistUsersState.IsAnilistUserBaneado(viewer.Id))
        {
            await interaction.Guild.BanMemberAsync(interaction.User.Id, TimeSpan.Zero, "Usuario baneado por AniList baneado");
            return;
        }

        UserApprovalAnilist userApproval = new()
        {
            IdDiscord = (long)interaction.User.Id,
            IdAnilist = viewer.Id,
            Name = viewer.Name,
            SiteUrl = viewer.SiteUrl,
            Avatar = viewer.AvatarMedium ?? string.Empty,
            Banner = viewer.BannerImage ?? string.Empty
        };

        DateTimeOffset monthBefore = DateTimeOffset.Now.AddMonths(-1);
        if (interaction.User.CreationTimestamp > monthBefore || viewer.CreatedAt > monthBefore)
        {
            DiscordButtonComponent aprobar = new(DiscordButtonStyle.Success, $"sync-true-{interaction.User.Id}", "Aprobar");
            DiscordButtonComponent denegar = new(DiscordButtonStyle.Danger, $"sync-false-{interaction.User.Id}", "Denegar");

            DiscordMessageBuilder msgBuilderApproval = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Yellow,
                    Title = "Vinculacion de AniList pendiente de aprobación",
                    Description =
                        $"El usuario {interaction.User.Mention} | Nombre: {Formatter.InlineCode(member.DisplayName)} | Id: {Formatter.InlineCode(interaction.User.Id.ToString())} ({Formatter.MaskedUrl("Link de su AniList", new Uri(viewer.SiteUrl))}) ha vinculado su cuenta de AniList pero su cuenta de Discord o AniList es muy reciente, por lo que debe ser aprobada manualmente por el staff.\n\n" +
                        $"- Discord: {interaction.User.CreationTimestamp:dd/MM/yyyy}\n" +
                        $"- AniList: {viewer.CreatedAt.ToLocalTime():dd/MM/yyyy}",
                })
                .AddActionRowComponent(aprobar, denegar);

            await usuariosAnilistRepository.AgregarUsuarioApproval(userApproval);
            await interaction.Guild.Channels[config.Channels.Moderacion].SendMessageAsync(msgBuilderApproval);

            await modalInteraction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Atencion",
                Description = "La cuenta debe ser vinculada manualmente por el staff.",
                Color = DiscordColor.Yellow
            }));

            return;
        }

        await TerminarVinculacion(client, interaction.User, member, interaction.Guild, userApproval);
        await modalInteraction.DeleteOriginalResponseAsync();
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
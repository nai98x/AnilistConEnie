using System.Text;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Model.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Helpers;

public class AnilistHelper(BotStateService botStateService, BotConfiguration config, DiscordHelper discordHelper, IAnilistClient anilistClient, IUsuariosAnilistRepository usuariosAnilistRepository, ILogger<AnilistHelper> logger)
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

        List<UsuarioAnilist> users = botStateService.Usuarios;
        if (!users.Where(u => (ulong)u.UserId == user.Id).Any())
        {
            List<UsuarioAnilist> newList = await usuariosAnilistRepository.GetListaUsuarios();
            UsuarioAnilist newUser = newList.First(u => (ulong)u.UserId == user.Id);
            users.Add(newUser);
            botStateService.SetUsuarios(users);
        }

        UserXp prevXp = botStateService.GetUserXp(user.Id);

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

    /// <summary>
    /// Arma el bloque de scores de los usuarios del servidor para <paramref name="media"/>: resuelve los
    /// miembros vinculados (rango Miembro+ y activos), consulta sus listas en AniList por lotes de 50 y
    /// devuelve el texto listo para la description del embed (promedio + scores + sin-score). Devuelve
    /// cadena vacía si nadie tiene el media en su lista.
    /// </summary>
    public async Task<string> GetServerScoresAsync(DiscordGuild guild, AnilistMedia media, bool includeUsersWithoutScore)
    {
        Dictionary<ulong, DiscordMember> miembros = [];
        await foreach (DiscordMember miembro in guild.GetAllMembersAsync())
            miembros[miembro.Id] = miembro;

        // Usuarios vinculados que están en el servidor con rango Miembro+ y no inactivos.
        List<UsuarioAnilist> usuariosServidor = [];
        foreach (UsuarioAnilist usuario in botStateService.Usuarios)
        {
            if (miembros.TryGetValue((ulong)usuario.UserId, out DiscordMember? miembro)
                && discordHelper.RangoAPartirDe(guild, miembro, RangoEnum.Miembro, true))
                usuariosServidor.Add(usuario);
        }

        // id de AniList -> usuario, para reasociar cada score con su DiscordMember.
        Dictionary<int, UsuarioAnilist> porAnilistId = new();
        foreach (UsuarioAnilist usuario in usuariosServidor)
        {
            if (TryGetAnilistId(usuario.AnilistURL, out int anilistId))
                porAnilistId[anilistId] = usuario;
        }

        if (porAnilistId.Count == 0) return string.Empty;

        List<string> conScore = [];
        List<string> sinScore = [];
        double suma100 = 0;
        int registros = 0;

        foreach (int[] lote in porAnilistId.Keys.Chunk(50))
        {
            IReadOnlyList<AnilistUserScore> scores = await anilistClient.GetMediaUserScoresAsync(media.Id, lote);

            foreach (AnilistUserScore entry in scores)
            {
                if (!porAnilistId.TryGetValue(entry.UserId, out UsuarioAnilist? usuario)
                    || !miembros.TryGetValue((ulong)usuario.UserId, out DiscordMember? miembro))
                    continue;

                string link = Formatter.MaskedUrl(miembro.DisplayName, new Uri(entry.UserSiteUrl));
                string progreso = FormatProgreso(media, entry.Progress);

                if (entry.HasScore)
                {
                    string score = AnilistMediaFormatter.UserScore(entry);
                    conScore.Add(entry.Status == "COMPLETED"
                        ? $"{link} - {score}\n"
                        : $"{link} - {score} {Formatter.InlineCode($"{entry.Status} - Progreso: {progreso}")}\n");

                    suma100 += entry.Score100;
                    registros++;
                }
                else if (includeUsersWithoutScore && !entry.Status.Equals("PLANNING", StringComparison.OrdinalIgnoreCase))
                {
                    sinScore.Add($"{link} - {Formatter.InlineCode($"{entry.Status} - Progreso: {progreso}")}\n");
                }
            }
        }

        return ComposeScores(conScore, sinScore, suma100, registros);
    }

    private static string ComposeScores(List<string> conScore, List<string> sinScore, double suma100, int registros)
    {
        if (conScore.Count == 0 && sinScore.Count == 0) return string.Empty;

        StringBuilder sb = new();

        if (conScore.Count > 0)
        {
            double promedio = suma100 / registros;
            sb.Append($"{Formatter.Bold("Promedio:")} {Math.Round(promedio, 2)}/100\n\n");

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

    /// <summary>Progreso del usuario sobre el total del media: "12/24" (o solo "12" si no se conoce el total).</summary>
    private static string FormatProgreso(AnilistMedia media, int? progress)
    {
        string actual = (progress ?? 0).ToString();
        int? total = media.Episodes ?? media.Chapters;
        return total is { } t ? $"{actual}/{t}" : actual;
    }

    /// <summary>Extrae el id numérico de AniList del final de la URL del perfil (ej. .../user/12345).</summary>
    private static bool TryGetAnilistId(string anilistUrl, out int anilistId)
    {
        anilistId = 0;
        if (string.IsNullOrEmpty(anilistUrl)) return false;

        string trimmed = anilistUrl.TrimEnd('/');
        int slash = trimmed.LastIndexOf('/');
        string segment = slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
        return int.TryParse(segment, out anilistId);
    }
}
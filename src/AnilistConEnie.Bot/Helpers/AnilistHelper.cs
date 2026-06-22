using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Model.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Helpers;

public class AnilistHelper(BotStateService botStateService, BotConfiguration config, DiscordHelper discordHelper, IUsuariosAnilistRepository usuariosAnilistRepository, ILogger<AnilistHelper> logger)
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
}
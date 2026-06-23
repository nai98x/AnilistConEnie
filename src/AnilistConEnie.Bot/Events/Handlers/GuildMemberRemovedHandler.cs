using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildMemberRemovedHandler(DiscordBotService discordBotService, XpState xpState, BotConfiguration config, DiscordHelper discordHelper, IUsuariosAnilistRepository usuariosAnilistRepository)
{
    public async Task Handle(DiscordClient client, GuildMemberRemovedEventArgs args)
    {
        if (args.Guild.Id != config.GuildId) return;
        
        if (!args.Member.IsBot && !discordBotService.Debug)
        {
            #region Mensaje de despedida
            UserXp rank = xpState.GetUserXp(args.Member.Id);

            if (args.Member.Roles.Any(x => x.Id == config.Roles.Miembro))
            {
                DiscordEmoji umaPoints = DiscordEmoji.FromGuildEmote(client, config.Emotes.UmaPoints.Get(discordBotService.Debug));
                DiscordEmoji worrysad = DiscordEmoji.FromGuildEmote(client, config.Emotes.Worrysad.Get(discordBotService.Debug));
                DiscordRole role = discordHelper.GetRoleByXp(args.Guild, rank.Total);

                if (rank.Total > DiscordHelper.GetXpFromRango(RangoEnum.Casual))
                {
                    DiscordEmbedBuilder embedBuilder = new();
                    embedBuilder.WithTitle($"Tenemos una baja");
                    embedBuilder.WithColor(DiscordColor.Red);
                    embedBuilder.WithDescription($"**{args.Member.DisplayName}** se fue del servidor {worrysad}, tenía {rank.Total} {umaPoints} y era rango {role.Mention}");
                    embedBuilder.WithThumbnail(args.Member.GuildAvatarUrl ?? args.Member.AvatarUrl);
                    embedBuilder.WithImageUrl("https://i.imgur.com/qMLynhU.jpeg");

                    DiscordGuild guild = client.Guilds[config.GuildId];
                    DiscordChannel channel = guild.Channels[config.Channels.General];

                    await channel.SendMessageAsync(embedBuilder.Build());
                }
            }
            #endregion

            #region Mensaje puerta
            UsuarioAnilist? usuario = await usuariosAnilistRepository.GetPerfil(args.Member.Id);
            if (usuario != null)
            {
                await discordHelper.BorrarMensajeUsuarioAnilist(args.Guild, usuario.MessageId);
                await usuariosAnilistRepository.DeleteAnilist(args.Member.Id);
                await discordHelper.GrabarLogUsuarioOutAnilist(args.Guild, args.Member, usuario, rank);
            }
            #endregion
        }
    }
}

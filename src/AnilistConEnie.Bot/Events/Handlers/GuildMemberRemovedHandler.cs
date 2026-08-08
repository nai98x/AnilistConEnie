using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Domain.Interfaces.Repositories;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildMemberRemovedHandler(DiscordBotService discordBotService, XpState xpState, BotConfiguration config, RangoRoles rangoRoles, DiscordLogService logService, AnilistService anilistService, IUsuariosRepository usuariosRepository)
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
                DiscordEmoji umaPoints = await DiscordEmojiHelper.GetApplicationEmojiAsync(client, config.Emotes.Bot.UmaPoints);
                DiscordEmoji worrysad = await DiscordEmojiHelper.GetApplicationEmojiAsync(client, config.Emotes.Bot.Worrysad);
                DiscordRole role = rangoRoles.GetRoleByXp(args.Guild, rank.Total);

                if (rank.Total > RangoXp.XpRequerida(RangoEnum.Casual))
                {
                    DiscordEmbedBuilder embedBuilder = new();
                    embedBuilder.WithTitle($"Tenemos una baja");
                    embedBuilder.WithColor(DiscordColor.Red);
                    embedBuilder.WithDescription($"**{args.Member.DisplayName}** se fue del servidor {worrysad}, tenía {rank.Total} {umaPoints} y era rango {role.Mention}");
                    embedBuilder.WithThumbnail(args.Member.AvatarUrlPreferido());
                    embedBuilder.WithImageUrl("https://i.imgur.com/qMLynhU.jpeg");

                    DiscordGuild guild = client.Guilds[config.GuildId];
                    DiscordChannel channel = guild.Channels[config.Channels.General];

                    await channel.SendMessageAsync(embedBuilder.Build());
                }
            }
            #endregion

            #region Mensaje puerta
            await anilistService.CancelarAprobacionPendiente(args.Guild, args.Member);

            UsuarioAnilist? usuario = await usuariosRepository.GetPerfil(args.Member.Id);
            if (usuario != null)
            {
                await logService.BorrarMensajeUsuarioAnilist(args.Guild, usuario.MessageId);
                await usuariosRepository.Desvincular(args.Member.Id);
                await logService.GrabarLogUsuarioOutAnilist(args.Guild, args.Member, usuario, rank);
            }
            #endregion
        }
    }
}

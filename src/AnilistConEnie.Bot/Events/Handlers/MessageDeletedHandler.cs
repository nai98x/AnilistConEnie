using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageDeletedHandler(
    DiscordBotService discordBotService,
    BotConfiguration config,
    DiscordLogService logService,
    IIntercambiosRepostRepository intercambiosRepostRepository)
{
    public async Task Handle(DiscordClient client, MessageDeletedEventArgs args)
    {
        if (args.Guild?.Id != config.GuildId) return;

        #region Intercambios Repost
        if (!discordBotService.Debug && args.Channel.ParentId == config.Channels.Intercambios.Reviews)
        {
            List<MensajeIntercambioRepost> mensajes = await intercambiosRepostRepository.GetMensajes(args.Message.Id);
            foreach (MensajeIntercambioRepost mensaje in mensajes)
            {
                try
                {
                    DiscordChannel repostChannel = args.Guild.Channels[mensaje.IdCanalMensajeRepost];
                    DiscordMessage repostMessage = await repostChannel.GetMessageAsync(mensaje.IdMensajeRepost, true);
                    await repostChannel.DeleteMessageAsync(repostMessage);
                }
                catch (Exception ex) { await logService.LogException(args.Guild, ex, "Intercambios repost - borrar mensaje"); }
            }

            if (mensajes.Count > 0)
                await intercambiosRepostRepository.DeleteMensaje(args.Message.Id);
        }
        #endregion
    }
}

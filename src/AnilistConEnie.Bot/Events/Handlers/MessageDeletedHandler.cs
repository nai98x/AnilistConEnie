using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageDeletedHandler(
    IServiceProvider services,
    BotConfiguration config,
    IIntercambiosRepostRepository intercambiosRepostRepository)
{
    public async Task Handle(DiscordClient client, MessageDeletedEventArgs args)
    {
        if (args.Guild?.Id != config.GuildId) return;

        DiscordBotService discordBotService = services.GetRequiredService<DiscordBotService>();

        #region Intercambios Repost
        if (!discordBotService.Debug && args.Channel.ParentId == config.Channels.Intercambios.Reviews)
        {
            MensajeIntercambioRepost? mensaje = await intercambiosRepostRepository.GetMensaje(args.Message.Id);
            if (mensaje != null)
            {
                DiscordChannel repostChannel = args.Guild.Channels[mensaje.IdCanalMensajeRepost];
                try
                {
                    DiscordMessage repostMessage = await repostChannel.GetMessageAsync(mensaje.IdMensajeRepost, true);
                    await repostChannel.DeleteMessageAsync(repostMessage);
                    await intercambiosRepostRepository.DeleteMensaje(args.Message.Id);
                }
                catch (Exception) { /* Ignored */ }
            }
        }
        #endregion
    }
}

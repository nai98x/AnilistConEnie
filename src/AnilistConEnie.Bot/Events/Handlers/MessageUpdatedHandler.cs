using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageUpdatedHandler(
    DiscordBotService discordBotService,
    BotConfiguration config,
    DiscordLogService logService,
    IIntercambiosRepostRepository intercambiosRepostRepository)
{
    public async Task Handle(DiscordClient client, MessageUpdatedEventArgs args)
    {
        if (args.Guild?.Id != config.GuildId) return;

        #region Intercambios Repost
        if (!discordBotService.Debug && args.Channel.ParentId == config.Channels.Intercambios.Reviews)
        {
            foreach (MensajeIntercambioRepost mensaje in await intercambiosRepostRepository.GetMensajes(args.Message.Id))
            {
                try
                {
                    DiscordChannel repostChannel = args.Guild.Channels[mensaje.IdCanalMensajeRepost];
                    DiscordMessage repostMessage = await repostChannel.GetMessageAsync(mensaje.IdMensajeRepost, true);
                    DiscordMessageBuilder edicion = new DiscordMessageBuilder()
                        .WithContent(repostMessage.Content)
                        .AddEmbed(new DiscordEmbedBuilder(repostMessage.Embeds[0]) { Description = args.Message.Content });

                    // Las imágenes adicionales del post van cada una en su propio embed.
                    foreach (DiscordEmbed imagen in repostMessage.Embeds.Skip(1))
                        edicion.AddEmbed(imagen);

                    await repostMessage.ModifyAsync(edicion);
                }
                catch (Exception ex) { await logService.LogException(args.Guild, ex, "Intercambios repost - editar mensaje"); }
            }
        }
        #endregion
    }
}

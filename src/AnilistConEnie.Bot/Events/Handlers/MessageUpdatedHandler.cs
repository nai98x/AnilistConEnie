using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageUpdatedHandler(
    IServiceProvider services,
    BotConfiguration config,
    IIntercambiosRepostRepository intercambiosRepostRepository)
{
    public async Task Handle(DiscordClient client, MessageUpdatedEventArgs args)
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
                    DiscordEmbed embed = repostMessage.Embeds[0];

                    DiscordEmbedBuilder newEmbed = new(embed);
                    newEmbed.Description = args.Message.Content;

                    await repostMessage.ModifyAsync(embed: newEmbed.Build());
                }
                catch (Exception) { /* Ignored */ }
            }
        }
        #endregion
    }
}

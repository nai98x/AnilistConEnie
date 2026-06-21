using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageReactionAddedHandler(IServiceProvider services, BotConfiguration config)
{
    public async Task Handle(DiscordClient client, MessageReactionAddedEventArgs args)
    {
        if (args.Guild?.Id != config.GuildId) return;

        MainService mainService = services.GetRequiredService<MainService>();
        SingletonService singletonService = services.GetRequiredService<SingletonService>();

        #region Control de reacciones para canal de sugerencias
        if (args.Channel.Id == config.Channels.Sugerencias
            && !args.Emoji.Equals(DiscordEmoji.FromUnicode(client, "✅"))
            && !args.Emoji.Equals(DiscordEmoji.FromUnicode(client, "❌"))
            && !mainService.Debug)
        {
            await args.Message.DeleteReactionAsync(args.Emoji, args.User);
        }
        #endregion

        #region Confesiones
        DiscordEmoji emote = DiscordEmoji.FromGuildEmote(client, 1134553504166465676);
        if (args.Channel.Id == 862408834693070901
            && singletonService.IsConfession(args.Message.Id)
            && args.Emoji.Id == emote.Id)
        {
            (bool guessed, ulong? messageId, ulong? userId) = singletonService.AddConfessionReaction(args.Message.Id, args.User.Id);
            if (guessed)
            {
                DiscordMember confessionUser = args.Guild!.Members[userId!.Value];
                DiscordMessage message = await args.Channel.GetMessageAsync(messageId!.Value);
                await message.RespondAsync(new DiscordEmbedBuilder()
                    .WithTitle("Confesión revelada")
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"**{args.Guild.Members[args.User.Id].DisplayName}** te sacó la ficha **{confessionUser.DisplayName}** {emote}")
                    .WithAuthor(name: confessionUser.DisplayName, iconUrl: confessionUser.AvatarUrl)
                );
            }
        }
        #endregion
    }
}

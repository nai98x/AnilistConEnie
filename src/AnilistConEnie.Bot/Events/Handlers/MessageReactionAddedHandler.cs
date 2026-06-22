using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageReactionAddedHandler(
    DiscordBotService discordBotService,
    BotStateService botStateService,
    BotConfiguration config)
{
    public async Task Handle(DiscordClient client, MessageReactionAddedEventArgs args)
    {
        if (args.Guild?.Id != config.GuildId) return;

        #region Control de reacciones para canal de sugerencias
        if (args.Channel.Id == config.Channels.Sugerencias
            && !args.Emoji.Equals(DiscordEmoji.FromUnicode(client, "✅"))
            && !args.Emoji.Equals(DiscordEmoji.FromUnicode(client, "❌"))
            && !discordBotService.Debug)
        {
            await args.Message.DeleteReactionAsync(args.Emoji, args.User);
        }
        #endregion

        #region Confesiones
        DiscordEmoji emote = DiscordEmoji.FromGuildEmote(client, config.Emotes.ConfessionReaction);
        if (args.Channel.Id == config.Channels.General
            && botStateService.IsConfession(args.Message.Id)
            && args.Emoji.Id == emote.Id)
        {
            (bool guessed, ulong? messageId, ulong? userId) = botStateService.AddConfessionReaction(args.Message.Id, args.User.Id);
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

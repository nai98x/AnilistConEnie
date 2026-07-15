using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageReactionAddedHandler(
    DiscordBotService discordBotService,
    ConfessionsState confessionsState,
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
        if (args.Channel.Id == config.Channels.General
            && confessionsState.IsConfession(args.Message.Id)
            && args.Emoji.Id == config.Emotes.Guild.ConfessionReaction)
        {
            (bool guessed, ulong? messageId, ulong? userId) = confessionsState.AddConfessionReaction(args.Message.Id, args.User.Id);
            if (guessed && args.Guild!.Members.TryGetValue(userId!.Value, out DiscordMember? confessionUser))
            {
                DiscordEmoji emote = DiscordEmoji.FromGuildEmote(client, config.Emotes.Guild.ConfessionReaction);
                string reactorName = args.Guild.Members.TryGetValue(args.User.Id, out DiscordMember? reactor)
                    ? reactor.DisplayName
                    : args.User.Username;
                DiscordMessage message = await args.Channel.GetMessageAsync(messageId!.Value);
                await message.RespondAsync(new DiscordEmbedBuilder()
                    .WithTitle("Confesión revelada")
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"**{reactorName}** te sacó la ficha **{confessionUser.DisplayName}** {emote}")
                    .WithAuthor(name: confessionUser.DisplayName, iconUrl: confessionUser.AvatarUrl)
                );
            }
        }
        #endregion
    }
}

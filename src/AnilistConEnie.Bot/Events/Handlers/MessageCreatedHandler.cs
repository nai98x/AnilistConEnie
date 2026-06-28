using AnilistConEnie.Application.Triggers;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageCreatedHandler(
    DiscordBotService discordBotService,
    MemberActivityState memberActivityState,
    XpState xpState,
    EmoteModeState emoteModeState,
    HackedAccountState hackedAccountState,
    TriggersState triggersState,
    BotConfiguration config,
    IIntercambiosRepostRepository intercambiosRepostRepository)
{
    public async Task Handle(DiscordClient client, MessageCreatedEventArgs args)
    {
        if (args.Guild.Id != config.GuildId) return;

        #region Deteccion de usuarios activos en el servidor
        if (!args.Author.IsBot && !discordBotService.Debug)
        {
            _ = memberActivityState.AddDailyActiveUser(args.Author.Id, client.Guilds[config.GuildId]);
        }
        #endregion

        #region Detección de mensajes con solo emojis para dar XP
        List<ulong> canalesSinXp =
        [
            config.Channels.ConfigBots,
            config.Channels.Tsuma,
            config.Channels.Mudae,
            config.Channels.ComandosCanal,
            config.Channels.ComandosForo
        ];

        if (!args.Author.IsBot
            && ((args.Channel is DiscordThreadChannel && !canalesSinXp.Contains(args.Channel.Parent.Id))
                || (args.Channel is not DiscordThreadChannel && !canalesSinXp.Contains(args.Channel.Id)))
            && !(args.Message.Content.StartsWith('<') && args.Message.Content.EndsWith('>') && args.Message.Content.Split(' ').Length == 1))
            xpState.AddMemberToObtainXp(args.Author.Id);
        #endregion

        #region YepMode
        if (emoteModeState.YepMode && args.Channel.Id == config.Channels.General)
            await args.Message.CreateReactionAsync(emoteModeState.Emote!);
        #endregion

        #region Autoban hacked accounts
        if (!args.Author.IsBot && (!discordBotService.Debug || (discordBotService.Debug && args.Author.Id == config.OwnerId)))
        {
            hackedAccountState.AddRecentUserMessage(args.Author.Id, args.Channel.Id, args.Message.Content);

            if (hackedAccountState.IsHackedAccount(args.Author.Id))
            {
                DiscordMember member = args.Guild.Members[args.Author.Id];

                if (member.Roles.All(x => x.Id != config.Roles.KamiSama))
                    await member.RemoveAsync("Autoban por cuenta hackeada");
            }
        }
        #endregion

        #region Triggers
        if ((!args.Author.IsBot && !string.IsNullOrEmpty(args.Message.Content) && args.Channel.Id != config.Channels.ConfigBots && !discordBotService.Debug)
            || (discordBotService.Debug && args.Message.Author?.Id == config.OwnerId))
        {
            IReadOnlyDictionary<string, Trigger> triggers = triggersState.GetActiveTriggers();

            foreach (Trigger trigger in TriggerMatcher.Aplicables(args.Message.Content, triggers))
            {
                DiscordMessageBuilder messageBuilder = new();

                if (!string.IsNullOrEmpty(trigger.Texto))
                    messageBuilder.WithContent(trigger.Texto);

                if (!string.IsNullOrEmpty(trigger.ImageUrl))
                    messageBuilder.AddEmbed(
                        new DiscordEmbedBuilder()
                            .WithImageUrl(trigger.ImageUrl)
                            .WithColor(DiscordEmojiHelper.GetColor())
                            .Build()
                    );

                await args.Message.RespondAsync(messageBuilder);
            }
        }
        #endregion

        #region Intercambios Repost
        if (!discordBotService.Debug && args.Channel.ParentId == config.Channels.Intercambios.Reviews)
        {
            DiscordThreadChannel? forumPost = args.Channel as DiscordThreadChannel;
            DiscordMember? autor = args.Message.Author as DiscordMember;
            List<DiscordAttachment> images = args.Message.Attachments.Where(x => x.MediaType?.StartsWith("image/") == true).Take(5).ToList();
            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder().WithContent($"{args.Message.JumpLink}");
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithDescription(args.Message.Content)
                .WithAuthor(autor?.DisplayName, iconUrl: autor?.GuildAvatarUrl ?? autor?.AvatarUrl)
                .WithColor(DiscordColor.Green);

            if (images.Count > 0)
            {
                bool first = true;
                foreach (DiscordAttachment image in images)
                {
                    if (first)
                    {
                        embed.ImageUrl = image.Url;
                        messageBuilder.AddEmbed(embed);
                        first = false;
                    }
                    else
                    {
                        messageBuilder.AddEmbed(new DiscordEmbedBuilder().WithImageUrl(image.Url!));
                    }
                }
            }
            else
            {
                messageBuilder.AddEmbed(embed);
            }

            Dictionary<string, ulong> tagChannelMap = new()
            {
                ["anime"]   = config.Channels.Intercambios.Anime,
                ["manga"]   = config.Channels.Intercambios.Manga,
                ["pelis"]   = config.Channels.Intercambios.Pelis,
                ["series"]  = config.Channels.Intercambios.Series,
                ["música"]  = config.Channels.Intercambios.Musica,
                ["fanarts"] = config.Channels.Intercambios.Fanarts,
            };

            foreach ((string tagName, ulong channelId) in tagChannelMap)
            {
                if (!forumPost!.AppliedTags.Any(x => x.Name.ToLowerInvariant() == tagName)) continue;

                DiscordChannel repostChannel = args.Guild.Channels[channelId];
                DiscordMessage msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                await intercambiosRepostRepository.SetMensaje(args.Message.ChannelId, args.Message.Id, msgRepost.ChannelId, msgRepost.Id);
            }
        }
        #endregion
    }
}

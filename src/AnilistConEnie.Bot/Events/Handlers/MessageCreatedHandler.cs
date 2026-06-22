using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class MessageCreatedHandler(
    DiscordBotService discordBotService,
    BotStateService botStateService,
    BotConfiguration config,
    IIntercambiosRepostRepository intercambiosRepostRepository)
{
    public async Task Handle(DiscordClient client, MessageCreatedEventArgs args)
    {
        if (args.Guild.Id != config.GuildId) return;

        #region Deteccion de usuarios activos en el servidor
        if (!args.Author.IsBot && !discordBotService.Debug)
        {
            _ = botStateService.AddDailyActiveUser(args.Author.Id, client.Guilds[config.GuildId]);
        }
        #endregion

        #region Detección de mensajes con solo emojis para dar XP
        List<ulong> canalesSinXp =
        [
            862473931192926228, // CONFIG BOTS
            1207444811833933824, // TSUMA
            862419212407668756, // MUDAE
            862417563325038602, // COMANDOS CANAL
            1207429768375574569 // COMANDOS FORO
        ];

        if (!args.Author.IsBot
            && ((args.Channel is DiscordThreadChannel && !canalesSinXp.Contains(args.Channel.Parent.Id))
                || (args.Channel is not DiscordThreadChannel && !canalesSinXp.Contains(args.Channel.Id)))
            && !(args.Message.Content.StartsWith('<') && args.Message.Content.EndsWith('>') && args.Message.Content.Split(' ').Length == 1))
            botStateService.AddMemberToObtainXp(args.Author.Id);
        #endregion

        #region YepMode
        if (botStateService.YepMode && args.Channel.Id == config.Channels.General)
            await args.Message.CreateReactionAsync(botStateService.Emote!);
        #endregion

        #region Autoban hacked accounts
        if (!args.Author.IsBot && (!discordBotService.Debug || (discordBotService.Debug && args.Author.Id == config.OwnerId)))
        {
            botStateService.AddRecentUserMessage(args.Author.Id, args.Channel.Id, args.Message.Content);

            if (botStateService.IsHackedAccount(args.Author.Id))
                await args.Guild.Members[args.Author.Id].BanAsync(TimeSpan.FromDays(1), "Autoban por cuenta hackeada");
        }
        #endregion

        #region Triggers
        if ((!args.Author.IsBot && !string.IsNullOrEmpty(args.Message.Content) && args.Channel.Id != config.Channels.ConfigBots && !discordBotService.Debug)
            || (discordBotService.Debug && args.Message.Author?.Id == config.OwnerId))
        {
            string mensajeOriginal = args.Message.Content.ToLower();
            IReadOnlyDictionary<string, Trigger> triggers = botStateService.GetActiveTriggers();
            List<KeyValuePair<string, Trigger>> matches = triggers.Where(x => mensajeOriginal.Contains(x.Key)).ToList();

            foreach (KeyValuePair<string, Trigger> trigger in matches)
            {
                bool validWithType = false;
                switch ((TipoTrigger)trigger.Value.Tipo)
                {
                    case TipoTrigger.TEXTO_EXACTO:
                        if (mensajeOriginal == trigger.Key) validWithType = true;
                        break;
                    case TipoTrigger.TERMINA_EN:
                        if (mensajeOriginal.EndsWith(trigger.Key)) validWithType = true;
                        break;
                    case TipoTrigger.EMPIEZA_CON:
                        if (mensajeOriginal.StartsWith(trigger.Key)) validWithType = true;
                        break;
                    case TipoTrigger.LIBRE:
                        validWithType = true;
                        break;
                }
                if (!validWithType) continue;

                DiscordMessageBuilder messageBuilder = new();

                if (!string.IsNullOrEmpty(trigger.Value.Texto))
                    messageBuilder.WithContent(trigger.Value.Texto);

                if (!string.IsNullOrEmpty(trigger.Value.ImageUrl))
                    messageBuilder.AddEmbed(
                        new DiscordEmbedBuilder()
                            .WithImageUrl(trigger.Value.ImageUrl)
                            .WithColor(DiscordHelper.GetColor())
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
            List<DiscordAttachment> images = args.Message.Attachments.Where(x => x.MediaType.StartsWith("image/")).Take(5).ToList();
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

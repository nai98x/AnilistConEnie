using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Firebase;
using AnilistConEnie.Helpers;
using AnilistConEnie.Repository;
using AnilistConEnie.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie;

public class Events(IServiceProvider services, ILogger<Events> logger)
{
    public async Task MessageCreated(DiscordClient client, MessageCreatedEventArgs args)
    {
        MainService mainService = services.GetRequiredService<MainService>();
        SingletonService singletonService = services.GetRequiredService<SingletonService>();
        
        if (args.Guild.Id == mainService.GuildId)
        {
            if (!args.Author.IsBot && !mainService.Debug)
            {
                _ = singletonService.AddDailyActiveUser(args.Author.Id, client.Guilds[mainService.GuildId]);
            }
            
            List<ulong> canalesSinXp =
            [
                862473931192926228, // CONFIG BOTS
                1207444811833933824, // TSUMA
                862419212407668756, // MUDAE
                862417563325038602, // COMANDOS CANAL
                1207429768375574569 // COMANDOS FORO
            ];
            
            if (!args.Author.IsBot && ((args.Channel is DiscordThreadChannel && !canalesSinXp.Contains(args.Channel.Parent.Id)) || (args.Channel is not DiscordThreadChannel && !canalesSinXp.Contains(args.Channel.Id)))
                                && !(args.Message.Content.StartsWith('<') && args.Message.Content.EndsWith('>') && args.Message.Content.Split(' ').Length == 1))
                singletonService.AddMemberToObtainXp(args.Author.Id);
            
            if (singletonService.YepMode && args.Channel.Id == mainService.CanalGeneralId)
                await args.Message.CreateReactionAsync(singletonService.Emote);
            
            #region Autoban hacked accounts
            if (!args.Author.IsBot && (!mainService.Debug || (mainService.Debug && args.Author.Id == 198212314892075009)))
            {
                singletonService.AddRecentUserMessage(args.Author.Id, args.Channel.Id, args.Message.Content);

                if (singletonService.IsHackedAccount(args.Author.Id))
                    await args.Guild.Members[args.Author.Id].BanAsync(TimeSpan.FromDays(1), "Autoban por cuenta hackeada");
            }
            #endregion
            
            #region Triggers
            if ((!args.Author.IsBot && !string.IsNullOrEmpty(args.Message.Content) && args.Channel.Id != mainService.ConfigBotsId && !mainService.Debug) || mainService.Debug && args.Message.Author?.Id == mainService.OwnerId)
            {
                string mensajeOriginal = args.Message.Content.ToLower();
                Dictionary<string, Trigger> triggers = singletonService.GetActiveTriggers();
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
            if (args.Guild?.Id == 862408834693070898 && !mainService.Debug)
            {
                if (args.Channel.ParentId == 1048075286626979861)
                {
                    DiscordThreadChannel? forumPost = args.Channel as DiscordThreadChannel;
                    DiscordMember? autor = args.Message.Author as DiscordMember;
                    List<DiscordAttachment> images = args.Message.Attachments.Where(x => x.MediaType.StartsWith("image/")).Take(5).ToList();
                    DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder().WithContent($"{args.Message.JumpLink}");
                    DiscordChannel repostChannel;
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

                    if (forumPost!.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "anime"))
                    {
                        repostChannel = args.Guild.Channels[862432891186839572];
                        DiscordMessage msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                        await IntercambiosRepostRepository.SetMensaje(args.Message.ChannelId, args.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                    }
                    if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "manga"))
                    {
                        repostChannel = args.Guild.Channels[882003534797742130];
                        DiscordMessage msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                        await IntercambiosRepostRepository.SetMensaje(args.Message.ChannelId, args.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                    }
                    if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "pelis"))
                    {
                        repostChannel = args.Guild.Channels[865319767967793152];
                        DiscordMessage msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                        await IntercambiosRepostRepository.SetMensaje(args.Message.ChannelId, args.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                    }
                    if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "series"))
                    {
                        repostChannel = args.Guild.Channels[1125578754790543380];
                        DiscordMessage msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                        await IntercambiosRepostRepository.SetMensaje(args.Message.ChannelId, args.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                    }
                    if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "música"))
                    {
                        repostChannel = args.Guild.Channels[862419584065732618];
                        DiscordMessage msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                        await IntercambiosRepostRepository.SetMensaje(args.Message.ChannelId, args.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                    }
                    if (forumPost.AppliedTags.Any(x => x.Name.ToLowerInvariant() == "fanarts"))
                    {
                        repostChannel = args.Guild.Channels[882004208721727498];
                        DiscordMessage msgRepost = await repostChannel.SendMessageAsync(messageBuilder);
                        await IntercambiosRepostRepository.SetMensaje(args.Message.ChannelId, args.Message.Id, msgRepost.ChannelId, msgRepost.Id);
                    }
                }
            }
            #endregion
        }
    }

    public async Task GuildDownloadCompleted(DiscordClient client, GuildDownloadCompletedEventArgs args)
    {
        MainService service = services.GetRequiredService<MainService>();
        service.SetChannels();
        
        service.SetInitialized();
        
        /*if (!Debug)
        {
            await Funciones.ManageBoosters(client.Guilds[862408834693070898]);
            await Funciones.ManageNewUsuarios(client.Guilds[862408834693070898]);
            await Funciones.ManageUsuariosActivos(client.Guilds[862408834693070898]);
            await Funciones.ManagSpamAccounts(client.Guilds[862408834693070898]);
            await Funciones.ClearInvitesRoleOnStartup(client.Guilds[862408834693070898]);
            await Funciones.ManageXPUserHistory(client.Guilds[862408834693070898]);
        }*/
    }
}

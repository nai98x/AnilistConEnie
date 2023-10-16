using AnilistConEnie.Infrastructure.Helpers.Interface;
using AnilistConEnie.Infrastructure.Repositories.Interface;
using Discord;
using Discord.WebSocket;

namespace AnilistConEnie.Infrastructure.Helpers.Impl
{
    public class IntercambioRepostHelper : IIntercambioRepostHelper
    {
        private readonly Settings _settings;
        private readonly IIntercambioRepostRepository _repository;

        public IntercambioRepostHelper(Settings settings, IIntercambioRepostRepository repository)
        {
            _settings = settings;
            _repository = repository;
        }

        public async Task AddPost(SocketMessage socketMessage)
        {
            if (socketMessage.Channel is SocketThreadChannel threadChannel && threadChannel.ParentChannel.Id == _settings.IntercambioForum)
            {
                var forumChannel = threadChannel.ParentChannel as SocketForumChannel;
                var forumPost = threadChannel;
                var autor = socketMessage.Author as IGuildUser;
                var image = socketMessage.Attachments.Where(x => x.ContentType.StartsWith("image/")).FirstOrDefault();
                SocketGuildChannel repostChannel;

                //var messageBuilder = new 
                var embed = new EmbedBuilder()
                    .WithDescription(socketMessage.Content)
                    .WithAuthor(socketMessage.Author)
                    .WithColor(Discord.Color.Green);

                if (image != null)
                {
                    embed.WithImageUrl(image.Url);
                }

                if (forumPost.AppliedTags.Any(x => x == _settings.AnimeIntercambioTag))
                {
                    repostChannel = threadChannel.Guild.Channels.First(x => x.Id == _settings.AnimeIntercambioChannel);
                    var sendChannel = repostChannel as ITextChannel;
                    var msgRepost = await sendChannel!.SendMessageAsync(embed: embed.Build());
                    await _repository.SetMensaje(threadChannel.Id, socketMessage.Id, msgRepost.Channel.Id, msgRepost.Id);
                }
                if (forumPost.AppliedTags.Any(x => x == _settings.MangaIntercambioTag))
                {
                    repostChannel = threadChannel.Guild.Channels.First(x => x.Id == _settings.MangaIntercambioChannel);
                    var sendChannel = repostChannel as ITextChannel;
                    var msgRepost = await sendChannel!.SendMessageAsync(embed: embed.Build());
                    await _repository.SetMensaje(threadChannel.Id, socketMessage.Id, msgRepost.Channel.Id, msgRepost.Id);
                }
                if (forumPost.AppliedTags.Any(x => x == _settings.PelisIntercambioTag))
                {
                    repostChannel = threadChannel.Guild.Channels.First(x => x.Id == _settings.PelisIntercambioChannel);
                    var sendChannel = repostChannel as ITextChannel;
                    var msgRepost = await sendChannel!.SendMessageAsync(embed: embed.Build());
                    await _repository.SetMensaje(threadChannel.Id, socketMessage.Id, msgRepost.Channel.Id, msgRepost.Id);
                }
                if (forumPost.AppliedTags.Any(x => x == _settings.SeriesIntercambioTag))
                {
                    repostChannel = threadChannel.Guild.Channels.First(x => x.Id == _settings.SeriesIntercambioChannel);
                    var sendChannel = repostChannel as ITextChannel;
                    var msgRepost = await sendChannel!.SendMessageAsync(embed: embed.Build());
                    await _repository.SetMensaje(threadChannel.Id, socketMessage.Id, msgRepost.Channel.Id, msgRepost.Id);
                }
                if (forumPost.AppliedTags.Any(x => x == _settings.MusicaIntercambioTag))
                {
                    repostChannel = threadChannel.Guild.Channels.First(x => x.Id == _settings.MusicaIntercambioChannel);
                    var sendChannel = repostChannel as ITextChannel;
                    var msgRepost = await sendChannel!.SendMessageAsync(embed: embed.Build());
                    await _repository.SetMensaje(threadChannel.Id, socketMessage.Id, msgRepost.Channel.Id, msgRepost.Id);
                }
            }
        }

        public async Task UpdatePost(Cacheable<IMessage, ulong> message, SocketMessage socketMessage, ISocketMessageChannel messageChannel)
        {
            if (messageChannel is SocketThreadChannel threadChannel && threadChannel.ParentChannel.Id == _settings.IntercambioForum)
            {
                var mensaje = await _repository.GetMensaje(message.Id);
                if (mensaje != null)
                {
                    var forumChannel = threadChannel.ParentChannel as SocketForumChannel;
                    var repostChannel = forumChannel?.Guild.Channels.First(x => x.Id == mensaje.IdCanalMensajeRepost) as ITextChannel;
                    if (repostChannel != null)
                    {
                        try
                        {
                            var repostMessage = await repostChannel.GetMessageAsync(mensaje.IdMensajeRepost) as IUserMessage;
                            var embed = repostMessage!.Embeds.First();

                            var newEmbed = embed.ToEmbedBuilder()
                                .WithDescription(socketMessage.Content)
                                .Build();

                            await repostMessage.ModifyAsync(x => x.Embed = newEmbed);
                        }
                        catch (Exception) { /* Ignored */}
                    }
                }
            }
        }

        public async Task DeletePost(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> messageChannel)
        {
            var channel = await messageChannel.GetOrDownloadAsync();
            if (channel is SocketThreadChannel threadChannel && threadChannel.ParentChannel.Id == _settings.IntercambioForum)
            {
                var mensaje = await _repository.GetMensaje(message.Id);
                if (mensaje != null)
                {
                    var forumChannel = threadChannel.ParentChannel as SocketForumChannel;
                    var repostChannel = forumChannel?.Guild.Channels.First(x => x.Id == mensaje.IdCanalMensajeRepost) as ITextChannel;
                    if (repostChannel != null)
                    {
                        try
                        {
                            var repostMessage = await repostChannel.GetMessageAsync(mensaje.IdMensajeRepost);
                            await repostChannel.DeleteMessageAsync(repostMessage);
                            await _repository.DeleteMensaje(message.Id);
                        }
                        catch (Exception) { /* Ignored */}
                    }
                }
            }
        }
    }
}

using Discord;
using Discord.WebSocket;

namespace AnilistConEnie.Infrastructure.Helpers.Interface
{
    public interface IIntercambioRepostHelper
    {
        Task AddPost(SocketMessage socketMessage);
        Task UpdatePost(Cacheable<IMessage, ulong> message, SocketMessage socketMessage, ISocketMessageChannel messageChannel);
        Task DeletePost(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> messageChannel); 
    }
}

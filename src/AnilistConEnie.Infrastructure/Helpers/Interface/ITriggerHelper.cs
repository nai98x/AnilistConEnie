using AnilistConEnie.Domain.Firebase;
using Discord.WebSocket;

namespace AnilistConEnie.Infrastructure.Helpers.Interface
{
    public interface ITriggerHelper
    {
        Task SetTriggers(bool enabled);
        Task SetTrigger(Trigger trigger);
        Task<Trigger?> EnableTrigger(string triggerName);
        Task<bool> DisableTrigger(string triggerName);
        Task ExecuteTrigger(SocketMessage socketMessage);
    }
}

using AnilistConEnie.Domain.Firebase;

namespace AnilistConEnie.Infrastructure.Repositories.Interface
{
    public interface ITriggerRepository
    {
        Task<List<Trigger>> GetTriggers(bool enabled);
        Task SetTrigger(Trigger trigger);
        Task<Trigger?> EnableTrigger(string triggerName);
        Task<bool> DisableTrigger(string triggerName);
    }
}

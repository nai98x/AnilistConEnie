using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface ITriggersRepository
{
    Task<List<Trigger>> GetTriggers(bool enabled);
    Task SetTrigger(Trigger trigger);
    Task<bool> DeleteTrigger(string triggerName);
    Task<Trigger?> EnableTrigger(string triggerName);
}

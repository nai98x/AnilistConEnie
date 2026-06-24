using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface ITriggersRepository
{
    Task<List<Trigger>> GetTriggers();
    Task SetTrigger(Trigger trigger);
    Task<bool> DeleteTrigger(string triggerName);
}

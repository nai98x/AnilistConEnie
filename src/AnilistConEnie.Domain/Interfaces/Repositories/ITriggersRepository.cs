using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface ITriggersRepository
{
    Task<List<Trigger>> GetLista();
    Task Upsert(Trigger trigger);
    Task<bool> Delete(string nombre);
}

using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface ITriggersRepository
{
    Task<List<Trigger>> GetLista();
    Task Upsert(Trigger trigger);
    Task<bool> Delete(string nombre);
}

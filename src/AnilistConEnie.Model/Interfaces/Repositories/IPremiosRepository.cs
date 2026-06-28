using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IPremiosRepository
{
    Task<List<Premio>> GetLista();
    Task Upsert(Premio premio);
}

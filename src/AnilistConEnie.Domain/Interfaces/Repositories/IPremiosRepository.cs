using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IPremiosRepository
{
    Task<List<Premio>> GetLista();
    Task Upsert(Premio premio);
}

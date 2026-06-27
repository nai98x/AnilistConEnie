using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IPremiosRepository
{
    Task<List<Premio>> GetLista();
    Task Upsert(Premio premio);
    Task SetPremio(int anio, Season season, string link);
}

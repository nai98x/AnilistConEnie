using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IPremiosRepository
{
    Task<List<Premio>> GetListaPremios();
    Task SetPremio(int anio, Season season, string link);
    Task RemovePremio(int anio, Season season);
}

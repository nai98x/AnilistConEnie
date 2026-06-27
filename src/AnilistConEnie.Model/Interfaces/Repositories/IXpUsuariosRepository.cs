using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IXpUsuariosRepository
{
    Task<UserXp?> Obtener(ulong userId);
    Task<List<UserXp>> ObtenerRanking();
    Task Insert(UserXp xp);
    Task Update(UserXp xp);
    Task AddRemove(ulong userId, UserXpDelta delta, XpOperation operation = XpOperation.Add);
}

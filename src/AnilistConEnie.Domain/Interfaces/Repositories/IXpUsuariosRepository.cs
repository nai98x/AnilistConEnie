using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IXpUsuariosRepository
{
    Task<UserXp?> Obtener(ulong userId);
    Task<List<UserXp>> ObtenerRanking();
    /// <summary>Aplica el delta y devuelve la XP del usuario ya actualizada.</summary>
    Task<UserXp> AddRemove(ulong userId, UserXpDelta delta, XpOperation operation = XpOperation.Add);

    /// <summary>
    /// Copia la xp del origen (actual e historial diario) al destino, sin modificar al origen. Si
    /// <paramref name="reemplazar"/> es <c>true</c> el destino queda igual al origen; si no, se suma a
    /// lo que ya tenía. Devuelve la xp del destino ya transferida.
    /// </summary>
    Task<UserXp> Transferir(ulong origen, ulong destino, bool reemplazar);
}

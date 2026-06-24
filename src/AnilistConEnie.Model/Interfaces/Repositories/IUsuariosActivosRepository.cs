using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IUsuariosActivosRepository
{
    Task<List<UsuarioActivo>> GetUsuariosActivos(HashSet<ulong> memberIds);
    Task<List<UsuarioActivo>> GetUsuariosInactivos(HashSet<ulong> memberIds, int months);
    Task SetUsuarioActividad(long userId);
}

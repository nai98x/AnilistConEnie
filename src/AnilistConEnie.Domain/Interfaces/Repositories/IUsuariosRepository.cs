using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IUsuariosRepository
{
    Task<Usuario?> Obtener(ulong userId);
    Task<List<UsuarioAnilist>> GetVinculados();
    Task<UsuarioAnilist?> GetPerfil(ulong userId);
    Task Upsert(ulong userId, string anilistUrl, long mensajeId);
    Task Desvincular(ulong userId);
    Task Asegurar(ulong userId);
    Task SetCumple(ulong userId, short? cumpleDia, short? cumpleMes);
    Task BorrarCumple(ulong userId);
    Task<List<Usuario>> GetCumples();
    Task<List<Usuario>> GetFechasEntrada();
    Task SetLastActivity(ulong userId, DateTime lastActivity);
    Task<List<UsuarioActivo>> GetUsuariosActivos(HashSet<ulong> memberIds, int months);
    Task<List<UsuarioActivo>> GetUsuariosInactivos(HashSet<ulong> memberIds, int months);
    Task Desactivar(ulong userId);
}

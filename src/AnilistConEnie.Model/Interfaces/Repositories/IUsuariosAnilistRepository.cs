using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IUsuariosAnilistRepository
{
    Task<List<UsuarioAnilist>> GetListaUsuarios();
    Task<UsuarioAnilist?> GetPerfil(ulong userId);
    Task SetAnilist(ulong userId, string anilistUrl, long messageId);
    Task DeleteAnilist(ulong userId);
    Task<List<UsuarioAnilistBaneado>> GetListaUsuariosBaneados();
    Task DeleteUsuarioBaneado(int userId);
    Task AgregarUsuarioBaneado(int userId);
    Task AgregarUsuarioApproval(UserApprovalAnilist user);
    Task EliminarUsuarioApproval(long userIdDiscord);
    Task<UserApprovalAnilist?> GetUsuarioApproval(long userIdDiscord);
    Task SetAnilistYumiko(int anilistId, ulong userId);
}

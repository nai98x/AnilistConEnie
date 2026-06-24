using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;

namespace AnilistConEnie.Bot.Services.State;

public class AnilistUsersState(IUsuariosAnilistRepository usuariosAnilistRepository)
{
    private List<UsuarioAnilist> _usuarios = [];
    private List<int> _anilistBaneados = [];

    public List<UsuarioAnilist> Usuarios => _usuarios;

    public void SetUsuarios(List<UsuarioAnilist> newUsers) => _usuarios = newUsers;

    public void FillAnilistBaneados(List<UsuarioAnilistBaneado> baneados) =>
        _anilistBaneados = [..baneados.Select(x => x.AnilistUserId)];

    public List<int> GetAnilistUsersBaneados() => _anilistBaneados;

    public async Task AddAnilistUserBaneado(int userId)
    {
        if (_anilistBaneados.Contains(userId)) return;
        _anilistBaneados.Add(userId);
        await usuariosAnilistRepository.AgregarUsuarioBaneado(userId);
    }

    public async Task RemoveAnilistUserBaneado(int userId)
    {
        if (!_anilistBaneados.Contains(userId)) return;
        _anilistBaneados.Remove(userId);
        await usuariosAnilistRepository.DeleteUsuarioBaneado(userId);
    }

    public bool IsAnilistUserBaneado(int userId) => _anilistBaneados.Contains(userId);
}

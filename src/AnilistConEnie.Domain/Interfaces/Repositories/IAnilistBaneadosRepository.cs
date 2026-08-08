using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IAnilistBaneadosRepository
{
    Task<List<UsuarioAnilistBaneado>> GetLista();
    Task<bool> Existe(int anilistUserId);
    Task Upsert(int anilistUserId);
    Task Delete(int anilistUserId);
}

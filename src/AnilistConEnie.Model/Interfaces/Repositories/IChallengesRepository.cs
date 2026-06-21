using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IChallengesRepository
{
    Task<List<Challenge>> GetLista();
    Task<List<ChallengeCompletado>> GetRankingUsuarios();
    Task Set(string nombre, string link, bool disponible, DateTime? vencimiento);
    Task SetUsuarioChallenge(string nombreChallenge, long userId, int xp, DateTimeOffset offset);
    Task<List<ChallengeCompletado>> GetListaUsuariosCompletaron(string nombreChallenge);
    Task<List<UsuarioChallenge>> GetChallengesUsuario(ulong userId);
    Task<List<UsuarioChallenge>> GetChallengesUsuariosNoDelServer(HashSet<ulong> memberIds);
}

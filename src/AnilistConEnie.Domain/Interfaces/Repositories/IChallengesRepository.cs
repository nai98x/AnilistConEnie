using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IChallengesRepository
{
    Task<List<Challenge>> GetLista();
    Task<long> Upsert(string nombre, string link, bool disponible, DateTime? vencimiento);
    Task UpsertCompletado(long challengeId, ulong userId, int xp, DateTimeOffset fecha);
    Task SetUsuarioChallenge(string nombre, ulong userId, int xp, DateTimeOffset fecha);
    Task<List<ChallengeCompletado>> GetListaUsuariosCompletaron(string nombre);
    Task<List<UsuarioChallenge>> GetChallengesUsuario(ulong userId);
}

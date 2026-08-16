using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IChallengesRepository
{
    Task<List<Challenge>> GetLista();
    Task<long> Upsert(string nombre, string link, bool disponible, DateTime? vencimiento, int maxCompletados);
    Task UpsertCompletado(long challengeId, ulong userId, int xp, DateTimeOffset fecha, int completados);

    /// <summary>
    /// Suma un completado al usuario acumulando la XP. Devuelve la cantidad de completados resultante,
    /// o null si el usuario ya llegó al máximo permitido por el challenge.
    /// </summary>
    Task<int?> SetUsuarioChallenge(string nombre, ulong userId, int xp, DateTimeOffset fecha);
    Task<List<ChallengeCompletado>> GetListaUsuariosCompletaron(string nombre);
    Task<List<UsuarioChallenge>> GetChallengesUsuario(ulong userId);
}

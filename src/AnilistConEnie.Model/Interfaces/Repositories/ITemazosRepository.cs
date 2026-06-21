using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface ITemazosRepository
{
    Task<List<Temazo>> GetTemazosByUser(ulong userId, int edicion);
    Task<List<Temazo>> GetTemazos(int edicion);
    Task<List<Temazo>> GetTemazosByVotes(int edicion);
    Task SetTemazo(ulong userId, int edicion, string nombre, int slot);
    Task SaveTemazoToDisk(string url, ulong userId, int slot);
    MemoryStream? GetTemazoFromDisk(ulong userId, int slot);
    Task<bool> SetVote(ulong userWhoIsVoting, ulong userId, int edicion, int slot);
}

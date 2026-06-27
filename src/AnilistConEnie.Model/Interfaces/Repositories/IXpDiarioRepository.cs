using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IXpDiarioRepository
{
    Task InsertBulk(ulong userId, IReadOnlyList<UserDailyXp> dias);
    Task<List<UserDailyXp>> ObtenerChart(ulong userId);
    Task Upsert(ulong userId, DateTime fecha, long xp);
}

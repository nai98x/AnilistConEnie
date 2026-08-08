using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IXpDiarioRepository
{
    Task InsertBulk(ulong userId, IReadOnlyList<UserDailyXp> dias);
    Task<List<UserDailyXp>> ObtenerChart(ulong userId);
    Task<List<UserDailyXp>> ObtenerBaseline(DateOnly fecha);
    Task Upsert(ulong userId, DateTime fecha, long xp);
    Task Snapshot(DateOnly fecha, IReadOnlyList<UserXp> usuarios);
}

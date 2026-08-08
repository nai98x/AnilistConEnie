namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface ITeiouCooldownRepository
{
    Task<DateTime?> Obtener(ulong userId);
    Task Upsert(ulong userId, DateTime cooldown);
}

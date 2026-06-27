namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface ITeiouCooldownRepository
{
    Task<DateTime?> Obtener(ulong userId);
    Task Upsert(ulong userId, DateTime cooldown);
}

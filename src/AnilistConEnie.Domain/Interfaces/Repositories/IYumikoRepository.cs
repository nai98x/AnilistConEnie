namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IYumikoRepository
{
    Task VincularAnilist(ulong userId, int anilistId);
}

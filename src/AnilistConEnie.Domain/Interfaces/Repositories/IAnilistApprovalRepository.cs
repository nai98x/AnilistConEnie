using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IAnilistApprovalRepository
{
    Task<UserApprovalAnilist?> Obtener(long idDiscord);
    Task Upsert(UserApprovalAnilist approval);
    Task Delete(long idDiscord);
}

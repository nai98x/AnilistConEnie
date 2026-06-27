using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IAnilistApprovalRepository
{
    Task<UserApprovalAnilist?> Obtener(long idDiscord);
    Task Upsert(UserApprovalAnilist approval);
    Task Delete(long idDiscord);
}

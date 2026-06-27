using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class AnilistApprovalRepository(DbConnectionFactory connectionFactory) : IAnilistApprovalRepository
{
    public async Task<UserApprovalAnilist?> Obtener(long idDiscord)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<UserApprovalAnilist>(
            "anilist_approval_obtener",
            new { p_id_discord = idDiscord },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Upsert(UserApprovalAnilist approval)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "anilist_approval_upsert",
            new
            {
                p_id_discord = approval.IdDiscord,
                p_id_anilist = approval.IdAnilist,
                p_avatar = approval.Avatar,
                p_site_url = approval.SiteUrl,
                p_name = approval.Name,
                p_banner = approval.Banner
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Delete(long idDiscord)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "anilist_approval_delete",
            new { p_id_discord = idDiscord },
            commandType: CommandType.StoredProcedure);
    }
}

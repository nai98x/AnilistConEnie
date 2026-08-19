using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

// Escribe en la base de Yumiko, no en la propia: solo el vínculo de AniList.
public class YumikoRepository(YumikoDbConnectionFactory connectionFactory) : IYumikoRepository
{
    public async Task VincularAnilist(ulong userId, int anilistId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "anilist_user_upsert",
            new { p_user_id = (long)userId, p_anilist_id = anilistId },
            commandType: CommandType.StoredProcedure);
    }
}

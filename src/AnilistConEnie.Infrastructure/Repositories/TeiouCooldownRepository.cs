using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class TeiouCooldownRepository(DbConnectionFactory connectionFactory) : ITeiouCooldownRepository
{
    public async Task<DateTime?> Obtener(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<DateTime?>(
            "teiou_cooldown_obtener",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Upsert(ulong userId, DateTime cooldown)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "teiou_cooldown_upsert",
            new { p_user_id = (long)userId, p_cooldown = cooldown },
            commandType: CommandType.StoredProcedure);
    }
}

using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class XpUsuariosRepository(DbConnectionFactory connectionFactory) : IXpUsuariosRepository
{
    public async Task<UserXp?> Obtener(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<UserXp>(
            "xp_usuario_obtener",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<UserXp>> ObtenerRanking()
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<UserXp>(
            "xp_usuario_ranking",
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task<UserXp> AddRemove(ulong userId, UserXpDelta delta, XpOperation operation = XpOperation.Add)
    {
        int sign = operation == XpOperation.Add ? 1 : -1;
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleAsync<UserXp>(
            "xp_usuario_add",
            new
            {
                p_user_id = (long)userId,
                p_total = delta.Total,
                p_booster = delta.Booster,
                p_challenges = delta.Challenges,
                p_eventos = delta.Eventos,
                p_intercambios = delta.Intercambios,
                p_otros = delta.Otros,
                p_sign = sign
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserXp> Transferir(ulong origen, ulong destino, bool reemplazar)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleAsync<UserXp>(
            "xp_transferir",
            new { p_origen = (long)origen, p_destino = (long)destino, p_reemplazar = reemplazar },
            commandType: CommandType.StoredProcedure);
    }
}

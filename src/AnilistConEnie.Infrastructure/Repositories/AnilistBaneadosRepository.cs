using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class AnilistBaneadosRepository(DbConnectionFactory connectionFactory) : IAnilistBaneadosRepository
{
    public async Task<List<UsuarioAnilistBaneado>> GetLista()
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<UsuarioAnilistBaneado>(
            "anilist_baneados_lista",
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task<bool> Existe(int anilistUserId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleAsync<bool>(
            "anilist_baneado_existe",
            new { p_anilist_user_id = anilistUserId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Upsert(int anilistUserId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "anilist_baneado_upsert",
            new { p_anilist_user_id = anilistUserId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Delete(int anilistUserId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "anilist_baneado_delete",
            new { p_anilist_user_id = anilistUserId },
            commandType: CommandType.StoredProcedure);
    }
}

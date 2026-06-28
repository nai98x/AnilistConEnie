using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class PremiosRepository(DbConnectionFactory connectionFactory) : IPremiosRepository
{
    public async Task<List<Premio>> GetLista()
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<Premio>(
            "premio_lista",
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task Upsert(Premio premio)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "premio_upsert",
            new { p_nombre = premio.Nombre, p_link = premio.Link, p_anio = premio.Year, p_orden = premio.Order },
            commandType: CommandType.StoredProcedure);
    }
}

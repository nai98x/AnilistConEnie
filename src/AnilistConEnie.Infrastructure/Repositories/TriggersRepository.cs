using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class TriggersRepository(DbConnectionFactory connectionFactory) : ITriggersRepository
{
    public async Task<List<Trigger>> GetLista()
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<Trigger>(
            "trigger_lista",
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task Upsert(Trigger trigger)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "trigger_upsert",
            new { p_nombre = trigger.Nombre, p_texto = trigger.Texto, p_image_url = trigger.ImageUrl, p_tipo = trigger.Tipo },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> Delete(string nombre)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<bool>(
            "trigger_delete",
            new { p_nombre = nombre },
            commandType: CommandType.StoredProcedure);
    }
}

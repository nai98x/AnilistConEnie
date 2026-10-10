using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class MensajesDiariosRepository(DbConnectionFactory connectionFactory) : IMensajesDiariosRepository
{
    public async Task Incrementar(ulong userId, DateOnly fecha)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "mensajes_diarios_incrementar",
            new { p_user_id = (long)userId, p_fecha = fecha },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Purgar(DateOnly fechaMinima)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "mensajes_diarios_purgar",
            new { p_fecha_minima = fechaMinima },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<UsuarioMensajes>> ObtenerTotales(DateOnly desde, DateOnly hasta)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<UsuarioMensajes>(
            "mensajes_diarios_totales",
            new { p_desde = desde, p_hasta = hasta },
            commandType: CommandType.StoredProcedure)).AsList();
    }
}

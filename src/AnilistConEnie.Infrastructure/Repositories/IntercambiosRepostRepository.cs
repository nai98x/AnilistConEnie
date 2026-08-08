using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class IntercambiosRepostRepository(DbConnectionFactory connectionFactory) : IIntercambiosRepostRepository
{
    public async Task<List<MensajeIntercambioRepost>> GetMensajes(ulong idMensajeHiloForo)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<MensajeIntercambioRepost>(
            "intercambios_repost_obtener",
            new { p_id_mensaje_hilo_foro = (long)idMensajeHiloForo },
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task Upsert(MensajeIntercambioRepost mensaje)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "intercambios_repost_upsert",
            new
            {
                p_id_mensaje_hilo_foro = (long)mensaje.IdMensajeHiloForo,
                p_id_canal_hilo_foro = (long)mensaje.IdCanalHiloForo,
                p_id_canal_mensaje_repost = (long)mensaje.IdCanalMensajeRepost,
                p_id_mensaje_repost = (long)mensaje.IdMensajeRepost
            },
            commandType: CommandType.StoredProcedure);
    }

    public Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost) =>
        Upsert(new MensajeIntercambioRepost
        {
            IdCanalHiloForo = idCanalHiloForo,
            IdMensajeHiloForo = idMensajeHiloForo,
            IdCanalMensajeRepost = idCanalMensajeRepost,
            IdMensajeRepost = idMensajeRepost
        });

    public async Task DeleteMensaje(ulong idMensajeHiloForo)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "intercambios_repost_delete",
            new { p_id_mensaje_hilo_foro = (long)idMensajeHiloForo },
            commandType: CommandType.StoredProcedure);
    }
}

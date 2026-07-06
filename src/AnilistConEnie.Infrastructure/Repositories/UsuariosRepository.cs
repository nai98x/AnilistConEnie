using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Application.Helpers;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class UsuariosRepository(DbConnectionFactory connectionFactory) : IUsuariosRepository
{
    public async Task<Usuario?> Obtener(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Usuario>(
            "usuario_obtener",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<UsuarioAnilist>> GetVinculados()
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<UsuarioAnilist>(
            "usuario_vinculados",
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task<UsuarioAnilist?> GetPerfil(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<UsuarioAnilist>(
            "usuario_perfil",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Upsert(ulong userId, string anilistUrl, long mensajeId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "usuario_upsert",
            new { p_user_id = (long)userId, p_anilist_url = anilistUrl, p_mensaje_id = mensajeId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Desvincular(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "usuario_desvincular",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetCumple(ulong userId, short? cumpleDia, short? cumpleMes)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "usuario_set_cumple",
            new { p_user_id = (long)userId, p_cumple_dia = cumpleDia, p_cumple_mes = cumpleMes },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Asegurar(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "usuario_asegurar",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task BorrarCumple(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "usuario_borrar_cumple",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<Usuario>> GetCumples()
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<Usuario>(
            "usuario_cumples",
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task SetLastActivity(ulong userId, DateTime lastActivity)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "usuario_set_last_activity",
            new { p_user_id = (long)userId, p_last_activity = lastActivity },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<UsuarioActivo>> GetUsuariosActivos(HashSet<ulong> memberIds)
    {
        DateTime desde = FechaCorte().AddMonths(-3);
        using var connection = await connectionFactory.OpenConnectionAsync();
        List<UsuarioActivo> usuarios = (await connection.QueryAsync<UsuarioActivo>(
            "usuario_activos",
            new { p_desde = desde },
            commandType: CommandType.StoredProcedure)).AsList();
        return usuarios.Where(x => memberIds.Contains((ulong)x.UserId)).ToList();
    }

    public async Task<List<UsuarioActivo>> GetUsuariosInactivos(HashSet<ulong> memberIds, int months)
    {
        DateTime hasta = FechaCorte().AddMonths(-months);
        using var connection = await connectionFactory.OpenConnectionAsync();
        List<UsuarioActivo> usuarios = (await connection.QueryAsync<UsuarioActivo>(
            "usuario_inactivos",
            new { p_hasta = hasta },
            commandType: CommandType.StoredProcedure)).AsList();
        return usuarios.Where(x => memberIds.Contains((ulong)x.UserId)).ToList();
    }

    // Fecha del día en hora argentina; el Kind Utc es solo para que Npgsql lo escriba en timestamptz sin convertir.
    private static DateTime FechaCorte() =>
        new(RelojServidor.Hoy.Year, RelojServidor.Hoy.Month, RelojServidor.Hoy.Day, 5, 0, 0, DateTimeKind.Utc);

    public async Task Desactivar(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "usuario_desactivar",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure);
    }
}

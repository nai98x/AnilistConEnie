using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class XpDiarioRepository(DbConnectionFactory connectionFactory) : IXpDiarioRepository
{
    public async Task InsertBulk(ulong userId, IReadOnlyList<UserDailyXp> dias)
    {
        DateOnly[] fechas = new DateOnly[dias.Count];
        long[] xps = new long[dias.Count];
        for (int i = 0; i < dias.Count; i++)
        {
            fechas[i] = DateOnly.FromDateTime(dias[i].Date);
            xps[i] = dias[i].Xp;
        }

        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "xp_diario_insert_bulk",
            new { p_user_id = (long)userId, p_fechas = fechas, p_xps = xps },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<UserDailyXp>> ObtenerChart(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<UserDailyXp>(
            "xp_diario_chart",
            new { p_user_id = (long)userId },
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task<List<UserDailyXp>> ObtenerBaseline(DateOnly fecha)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<UserDailyXp>(
            "xp_diario_baseline",
            new { p_fecha = fecha },
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task Upsert(ulong userId, DateTime fecha, long xp)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "xp_diario_upsert",
            new { p_user_id = (long)userId, p_fecha = DateOnly.FromDateTime(fecha), p_xp = xp },
            commandType: CommandType.StoredProcedure);
    }

    public async Task Snapshot(DateOnly fecha, IReadOnlyList<UserXp> usuarios)
    {
        if (usuarios.Count == 0) return;

        long[] userIds = new long[usuarios.Count];
        long[] xps = new long[usuarios.Count];
        for (int i = 0; i < usuarios.Count; i++)
        {
            userIds[i] = usuarios[i].UserId;
            xps[i] = usuarios[i].Total;
        }

        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "xp_diario_snapshot",
            new { p_fecha = fecha, p_user_ids = userIds, p_xps = xps },
            commandType: CommandType.StoredProcedure);
    }
}

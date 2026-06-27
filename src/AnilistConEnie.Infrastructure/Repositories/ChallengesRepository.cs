using System.Data;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces.Repositories;
using Dapper;

namespace AnilistConEnie.Infrastructure.Repositories;

public class ChallengesRepository(DbConnectionFactory connectionFactory) : IChallengesRepository
{
    public async Task<List<Challenge>> GetLista()
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<Challenge>(
            "challenge_lista",
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task<long> Upsert(string nombre, string link, bool disponible, DateTime? vencimiento)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return await connection.QuerySingleAsync<long>(
            "challenge_upsert",
            new { p_nombre = nombre, p_link = link, p_disponible = disponible, p_vencimiento = vencimiento },
            commandType: CommandType.StoredProcedure);
    }

    public async Task UpsertCompletado(long challengeId, ulong userId, int xp, DateTimeOffset fecha)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "challenge_usuario_upsert",
            new { p_challenge_id = challengeId, p_user_id = (long)userId, p_xp = xp, p_fecha = fecha },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetUsuarioChallenge(string nombre, ulong userId, int xp, DateTimeOffset fecha)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "challenge_usuario_completar",
            new { p_nombre = nombre, p_user_id = (long)userId, p_xp = xp, p_fecha = fecha },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<ChallengeCompletado>> GetListaUsuariosCompletaron(string nombre)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<ChallengeCompletado>(
            "challenge_completados",
            new { p_nombre = nombre },
            commandType: CommandType.StoredProcedure)).AsList();
    }

    public async Task<List<UsuarioChallenge>> GetChallengesUsuario(ulong userId)
    {
        using var connection = await connectionFactory.OpenConnectionAsync();
        return (await connection.QueryAsync<UsuarioChallenge, Challenge, UsuarioChallenge>(
            "challenge_usuario_lista",
            (usuarioChallenge, challenge) =>
            {
                usuarioChallenge.Challenge = challenge;
                return usuarioChallenge;
            },
            new { p_user_id = (long)userId },
            splitOn: "nombre",
            commandType: CommandType.StoredProcedure)).AsList();
    }
}

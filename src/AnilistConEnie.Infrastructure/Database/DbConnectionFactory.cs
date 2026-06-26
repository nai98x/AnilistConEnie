using System.Data;
using Dapper;
using Npgsql;

namespace AnilistConEnie.Infrastructure.Database;

public class DbConnectionFactory(string connectionString)
{
    public async Task<IDbConnection> OpenConnectionAsync()
    {
        NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<string> TestConnectionAsync()
    {
        using IDbConnection connection = await OpenConnectionAsync();
        return await connection.QuerySingleAsync<string>("SELECT version();");
    }
}

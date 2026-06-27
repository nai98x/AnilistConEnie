using System.Data;
using Dapper;
using Npgsql;

namespace AnilistConEnie.Infrastructure.Database;

public class DbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    static DbConnectionFactory()
    {
        // Las columnas son snake_case y los POCOs PascalCase: que Dapper mapee user_id → UserId, etc.
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // CommandType.StoredProcedure invoca las funciones con SELECT * FROM (lo genera el driver), no CALL:
        // así los repos solo nombran el SP, sin SQL en el código de la app.
        AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

        // Dapper (esta versión) no soporta DateOnly como parámetro escalar: lo mapeamos a un date.
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }
    }

    public DbConnectionFactory(string connectionString)
    {
        _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
    }

    public async Task<IDbConnection> OpenConnectionAsync() => await _dataSource.OpenConnectionAsync();

    // Abrir la conexión hace el handshake TCP + auth: si el server es inalcanzable, lanza. Sin SQL.
    public async Task EnsureConnectionAsync()
    {
        using IDbConnection connection = await _dataSource.OpenConnectionAsync();
    }
}

using System.Data;
using Microsoft.Extensions.Configuration;
#if (UseSqlite)
using Microsoft.Data.Sqlite;
#elif (UsePostgres)
using Npgsql;
#endif

namespace CleanArchWebApi.Infrastructure.Repositories.Dapper;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
#if (UseSqlite)
        _connectionString = configuration.GetConnectionString("Default")!;
#elif (UseSqlServer)
        _connectionString = configuration.GetConnectionString("CleanArchWebApi")!;
#elif (UsePostgres)
        _connectionString = configuration.GetConnectionString("CleanArchWebApi")!;
#endif
    }

    public IDbConnection CreateConnection()
    {
#if (UseSqlite)
        return new SqliteConnection(_connectionString);
#elif (UseSqlServer)
        return new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
#elif (UsePostgres)
        return new NpgsqlConnection(_connectionString);
#endif
    }
}

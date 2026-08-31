using System.Data;
using Dapper;
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

    // Dapper has no migration story of its own, so this stands in for what EF Core's
    // Database.MigrateAsync() gives that provider for free on a fresh database.
    public async Task InitializeSchemaAsync()
    {
        using var connection = CreateConnection();
#if (UseSqlite)
        await connection.ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS TodoItems (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                IsComplete INTEGER NOT NULL
            )
            """
        );
#elif (UseSqlServer)
        await connection.ExecuteAsync(
            """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TodoItems')
            BEGIN
                CREATE TABLE TodoItems (
                    Id NVARCHAR(36) PRIMARY KEY,
                    Title NVARCHAR(200) NOT NULL,
                    IsComplete BIT NOT NULL
                )
            END
            """
        );
#elif (UsePostgres)
        await connection.ExecuteAsync(
            """
            CREATE TABLE IF NOT EXISTS TodoItems (
                Id TEXT PRIMARY KEY,
                Title VARCHAR(200) NOT NULL,
                IsComplete BOOLEAN NOT NULL
            )
            """
        );
#endif
    }
}

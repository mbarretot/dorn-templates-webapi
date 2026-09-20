#if (!IncludeSample)
#if (UseSqlServer)
using Testcontainers.MsSql;
#elif (UsePostgres)
using Testcontainers.PostgreSql;
#endif
#if (UseDapper)
using CleanArchWebApi.Infrastructure.Repositories.Dapper;
using Microsoft.Extensions.Configuration;
#endif

namespace CleanArchWebApi.Integration.Tests;

public sealed class PersistenceConnectivityTests : IAsyncLifetime
{
#if (UseSqlite)
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"{Guid.NewGuid()}.db"
    );
#elif (UseSqlServer)
    // Same image tag as docker-compose.SqlServer.yml, kept in sync deliberately.
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-latest"
    ).Build();
#elif (UsePostgres)
    // Same image tag as docker-compose.Postgres.yml, kept in sync deliberately.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();
#endif

    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
#if (UseSqlite)
        _connectionString = $"Data Source={_databasePath}";
        await Task.CompletedTask;
#else
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
#endif
    }

    [Fact]
    public async Task Connection_ToTheRealProvider_IsAccepted()
    {
#if (UseEfCore)
#if (UseSqlite)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connectionString)
            .Options;
#elif (UseSqlServer)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
#elif (UsePostgres)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
#endif
        await using var dbContext = new ApplicationDbContext(options, Substitute.For<IPublisher>());

        await dbContext.Database.MigrateAsync();

        Assert.True(await dbContext.Database.CanConnectAsync());
#else
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
#if (UseSqlite)
                    ["ConnectionStrings:Default"] = _connectionString,
#else
                    ["ConnectionStrings:CleanArchWebApi"] = _connectionString,
#endif
                }
            )
            .Build();

        using var connection = new DapperContext(configuration).CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";

        Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar()));
        await Task.CompletedTask;
#endif
    }

    public async Task DisposeAsync()
    {
#if (UseSqlite)
        // Microsoft.Data.Sqlite pools native connections by file path, so the file can stay locked on Windows
        // until SqliteConnection.ClearAllPools() is called.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        await Task.CompletedTask;
#else
        await _container.DisposeAsync();
#endif
    }
}
#endif

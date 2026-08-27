#if (UseSqlServer)
using Testcontainers.MsSql;
#elif (UsePostgres)
using Testcontainers.PostgreSql;
#endif

namespace CleanArchWebApi.Integration.Tests.Todos;

/// <summary>
/// Boots the selected real provider (Testcontainers SQL Server or temp-file SQLite) and applies EF Core migrations.
/// </summary>
public sealed class PersistenceTestFixture : IAsyncLifetime
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

    public ApplicationDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
#if (UseSqlite)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
#elif (UseSqlServer)
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;
#elif (UsePostgres)
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
#endif

        DbContext = new ApplicationDbContext(options, Substitute.For<IPublisher>());
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();

#if (UseSqlite)
        // Microsoft.Data.Sqlite pools native connections by file path, so disposing DbContext can leave
        // the database locked on Windows until SqliteConnection.ClearAllPools() is called.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
#elif (UseSqlServer)
        await _container.DisposeAsync();
#elif (UsePostgres)
        await _container.DisposeAsync();
#endif
    }
}

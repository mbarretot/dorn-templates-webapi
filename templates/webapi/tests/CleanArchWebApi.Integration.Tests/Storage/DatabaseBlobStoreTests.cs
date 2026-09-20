#if (UseDatabaseBlobs)
#if (UseSqlServer)
using Testcontainers.MsSql;
#elif (UsePostgres)
using Testcontainers.PostgreSql;
#endif
using CleanArchWebApi.Application.Common.Storage;
using CleanArchWebApi.Infrastructure.Storage;
#if (UseDapper)
using CleanArchWebApi.Infrastructure.Repositories.Dapper;
using Microsoft.Extensions.Configuration;
#endif

namespace CleanArchWebApi.Integration.Tests.Storage;

/// <summary>Round-trips blobs through the selected real provider (Testcontainers SQL Server/PostgreSQL, or a temp-file SQLite database).</summary>
public sealed class DatabaseBlobStoreTests : IAsyncLifetime
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
#if (UseEfCore)
    private ApplicationDbContext _dbContext = null!;
#endif
    private IBlobStore _store = null!;

    public async Task InitializeAsync()
    {
#if (UseSqlite)
        var connectionString = $"Data Source={_databasePath}";
#else
        await _container.StartAsync();
        var connectionString = _container.GetConnectionString();
#endif
#if (UseEfCore)
#if (UseSqlite)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connectionString)
            .Options;
#elif (UseSqlServer)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
#elif (UsePostgres)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
#endif
        _dbContext = new ApplicationDbContext(options, Substitute.For<IPublisher>());
        await _dbContext.Database.MigrateAsync();
        _store = new EfCoreBlobStore(_dbContext);
#else
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
#if (UseSqlite)
                    ["ConnectionStrings:Default"] = connectionString,
#else
                    ["ConnectionStrings:CleanArchWebApi"] = connectionString,
#endif
                }
            )
            .Build();
        var context = new DapperContext(configuration);
        await context.InitializeSchemaAsync();
        _store = new DapperBlobStore(context);
#endif
    }

    private static MemoryStream Bytes(params byte[] bytes) => new(bytes);

    private static async Task<byte[]> ReadAsync(BlobContent blob)
    {
        await using var content = blob.Content;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    [Fact]
    public async Task Save_ThenOpen_ReturnsTheBytesTheContentTypeAndTheSize()
    {
        // Every byte value, so a text-encoding or truncation bug shows up.
        var payload = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();

        await _store.SaveAsync("documents/all-bytes.bin", Bytes(payload), "application/x-test");

        var blob = await _store.OpenAsync("documents/all-bytes.bin");

        Assert.NotNull(blob);
        Assert.Equal("application/x-test", blob.ContentType);
        Assert.Equal(payload.Length, blob.Size);
        Assert.Equal(payload, await ReadAsync(blob));
    }

    [Fact]
    public async Task Save_WithALargePayload_RoundTrips()
    {
        var payload = new byte[512 * 1024];
        new Random(42).NextBytes(payload);

        await _store.SaveAsync("large.bin", Bytes(payload), "application/octet-stream");

        var blob = await _store.OpenAsync("large.bin");

        Assert.NotNull(blob);
        Assert.Equal(payload, await ReadAsync(blob));
    }

    [Fact]
    public async Task Save_WithAnExistingName_ReplacesTheContentAndTheContentType()
    {
        await _store.SaveAsync("replace.txt", Bytes(1, 2, 3), "text/plain");
        await _store.SaveAsync("replace.txt", Bytes(9), "application/json");

        var blob = await _store.OpenAsync("replace.txt");

        Assert.NotNull(blob);
        Assert.Equal("application/json", blob.ContentType);
        Assert.Equal(new byte[] { 9 }, await ReadAsync(blob));
    }

    [Fact]
    public async Task Save_WithoutAContentType_UsesTheDefault()
    {
        await _store.SaveAsync("untyped.bin", Bytes(1), "");

        var blob = await _store.OpenAsync("untyped.bin");

        Assert.NotNull(blob);
        await using var content = blob.Content;
        Assert.Equal(BlobRules.DefaultContentType, blob.ContentType);
    }

    [Fact]
    public async Task Open_WithAnUnknownName_ReturnsNull()
    {
        Assert.Null(await _store.OpenAsync("missing.txt"));
    }

    [Fact]
    public async Task ExistsAndDelete_FollowTheBlobLifecycle()
    {
        Assert.False(await _store.ExistsAsync("lifecycle.txt"));
        Assert.False(await _store.DeleteAsync("lifecycle.txt"));

        await _store.SaveAsync("lifecycle.txt", Bytes(1), "text/plain");

        Assert.True(await _store.ExistsAsync("lifecycle.txt"));
        Assert.True(await _store.DeleteAsync("lifecycle.txt"));
        Assert.False(await _store.ExistsAsync("lifecycle.txt"));
        Assert.Null(await _store.OpenAsync("lifecycle.txt"));
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("a'; DROP TABLE Blobs; --")]
    public async Task EveryOperation_WithAnInvalidName_Throws(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _store.SaveAsync(name, Bytes(1), "text/plain")
        );
        await Assert.ThrowsAsync<ArgumentException>(() => _store.OpenAsync(name));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.ExistsAsync(name));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.DeleteAsync(name));
    }

    public async Task DisposeAsync()
    {
#if (UseEfCore)
        await _dbContext.DisposeAsync();
#endif
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

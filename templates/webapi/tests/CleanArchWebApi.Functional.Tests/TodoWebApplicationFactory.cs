#if (UseCustomAuth)
using CleanArchWebApi.Functional.Tests.Auth;
#endif

namespace CleanArchWebApi.Functional.Tests;

/// <summary>ConfigurePersistence/InitializePersistenceAsync/DisposePersistenceAsync are implemented per ORM/provider in sibling partials.</summary>
public sealed partial class TodoWebApplicationFactory
    : WebApplicationFactory<Program>,
        IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"{Guid.NewGuid()}.db"
    );

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
#if (UseCustomAuth)
        builder
            .UseSetting("Jwt:SigningKey", AuthWebApplicationFactory.SigningKey)
            .UseSetting("Jwt:Issuer", AuthWebApplicationFactory.Issuer)
            .UseSetting("Jwt:Audience", AuthWebApplicationFactory.Audience)
            .UseSetting("Jwt:LifetimeMinutes", "60");
#endif
        ConfigurePersistence(builder);
    }

    partial void ConfigurePersistence(IWebHostBuilder builder);

    Task IAsyncLifetime.InitializeAsync() => InitializePersistenceAsync();

    private partial Task InitializePersistenceAsync();

    Task IAsyncLifetime.DisposeAsync() => DisposePersistenceAsync();

    private partial Task DisposePersistenceAsync();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Microsoft.Data.Sqlite pools native connections by file path, so disposing the host can leave
        // the database locked on Windows until SqliteConnection.ClearAllPools() is called.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

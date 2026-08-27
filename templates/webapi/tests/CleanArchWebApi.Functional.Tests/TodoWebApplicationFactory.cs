#if (UseCustomAuth)
using CleanArchWebApi.Functional.Tests.Auth;
#endif

namespace CleanArchWebApi.Functional.Tests;

/// <summary>
/// Uses a unique SQLite temp file for the HTTP pipeline tier; this avoids Windows locking races and stays provider-independent.
/// </summary>
public sealed class TodoWebApplicationFactory : WebApplicationFactory<Program>
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
        builder.ConfigureServices(services =>
        {
            // AddDbContext appends config via Add, not TryAdd — removing only
            // DbContextOptions<T> leaves Program.cs's original provider registered alongside
            // this one, and EF Core throws seeing two providers. Remove both.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseSqlite($"Data Source={_databasePath}")
                    // In a --database sqlserver generation, the checked-in migrations were
                    // snapshotted against SQL Server, so EF's model differ flags a false
                    // "pending changes" warning when evaluated against SQLite. Documented
                    // suppression: https://aka.ms/efcore-docs-pending-changes.
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning)
                    )
            );
        });
    }

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

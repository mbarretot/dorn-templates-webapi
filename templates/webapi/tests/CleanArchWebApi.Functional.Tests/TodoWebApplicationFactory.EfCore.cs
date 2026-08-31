namespace CleanArchWebApi.Functional.Tests;

public sealed partial class TodoWebApplicationFactory
{
    partial void ConfigurePersistence(IWebHostBuilder builder)
    {
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

    private partial Task InitializePersistenceAsync() => Task.CompletedTask;

    private partial Task DisposePersistenceAsync() => Task.CompletedTask;
}

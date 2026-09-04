#if (UseCustomAuth)
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace CleanArchWebApi.Functional.Tests.Auth;

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string DemoEmail = "demo@example.com";

    /// <summary>Authenticated but granted only <see cref="Permissions.TodosRead"/> -- proves 403 (not 401) for the permissions it lacks.</summary>
    public const string LimitedEmail = "limited@example.com";
    public const string SigningKey = "test-signing-key-32-bytes-long-12345";
    public const string Issuer = "https://test-issuer.example.com";
    public const string Audience = "test-api";

    public string DemoPassword { get; }
    public string LimitedPassword { get; }

    public AuthWebApplicationFactory()
    {
        DemoPassword = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)
        );
        LimitedPassword = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)
        );
    }

    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"{Guid.NewGuid()}.db"
    );

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseSetting("Jwt:SigningKey", SigningKey)
            .UseSetting("Jwt:Issuer", Issuer)
            .UseSetting("Jwt:Audience", Audience)
            .UseSetting("Jwt:LifetimeMinutes", "60")
            .UseSetting("AuthSeed:DemoEmail", string.Empty);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseSqlite($"Data Source={_databasePath}")
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning)
                    )
            );
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        dbContext.Users.Add(
            new AppUser
            {
                Email = DemoEmail,
                UserName = DemoEmail,
                NormalizedEmail = DemoEmail.ToUpperInvariant(),
                NormalizedUserName = DemoEmail.ToUpperInvariant(),
                PasswordHash = hasher.HashPassword(null!, DemoPassword),
                Permissions = [.. Permissions.All],
            }
        );
        dbContext.Users.Add(
            new AppUser
            {
                Email = LimitedEmail,
                UserName = LimitedEmail,
                NormalizedEmail = LimitedEmail.ToUpperInvariant(),
                NormalizedUserName = LimitedEmail.ToUpperInvariant(),
                PasswordHash = hasher.HashPassword(null!, LimitedPassword),
                Permissions = [Permissions.TodosRead],
            }
        );
        dbContext.SaveChanges();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
#endif

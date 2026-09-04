#if (UseCustomAuth)
using CleanArchWebApi.Functional.Tests.Auth;
#endif
#if (UseAuth)
using System.Security.Claims;
using System.Text.Encodings.Web;
using CleanArchWebApi.Application.Common.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
#if (UseAuth)
        // This fixture exercises Todo CRUD behavior, not authorization (see Auth/TodoAuthorizationTests.cs for
        // that), so it replaces the real auth scheme with one that always succeeds and grants every permission
        // instead of round-tripping a real login/token per request.
        builder.ConfigureServices(services =>
        {
            services
                .AddAuthentication(AlwaysAuthenticatedHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, AlwaysAuthenticatedHandler>(
                    AlwaysAuthenticatedHandler.SchemeName,
                    _ => { }
                );
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = AlwaysAuthenticatedHandler.SchemeName;
                options.DefaultAuthenticateScheme = AlwaysAuthenticatedHandler.SchemeName;
                options.DefaultChallengeScheme = AlwaysAuthenticatedHandler.SchemeName;
            });
        });
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

#if (UseAuth)
internal sealed class AlwaysAuthenticatedHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AlwaysAuthenticated";

    public AlwaysAuthenticatedHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            Permissions.All.Select(permission => new Claim(Permissions.ClaimType, permission)),
            Scheme.Name
        );
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
#endif

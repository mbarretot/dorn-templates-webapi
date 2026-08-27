#if (UseAzureAdAuth)
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchWebApi.Functional.Tests.Auth;

/// <summary>
/// Microsoft.Identity.Web's dynamic IssuerValidator/AudienceValidator override any static
/// PostConfigure attempt, so this replaces the auth scheme entirely with
/// <see cref="AzureAdTestAuthHandler"/> instead of trying to fake them.
/// </summary>
public sealed class AzureAdWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string SchemeName = "TestBearer";
    public const string SigningKey = "azure-ad-test-signing-key-32-bytes-1234";
    public const string Issuer = "https://test-issuer.example.com";
    public const string Audience = "test-api";

    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"{Guid.NewGuid()}.db"
    );

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

            services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, AzureAdTestAuthHandler>(
                    SchemeName,
                    _ => { }
                );
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = SchemeName;
                options.DefaultAuthenticateScheme = SchemeName;
                options.DefaultChallengeScheme = SchemeName;
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
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

internal sealed class AzureAdTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public AzureAdTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (
            !Request.Headers.TryGetValue("Authorization", out var header)
            || !header.ToString().StartsWith("Bearer ", StringComparison.Ordinal)
        )
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = header.ToString()["Bearer ".Length..];
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return Task.FromResult(AuthenticateResult.Fail("Malformed token."));
        }

        var expectedSignature = ComputeSignature(
            $"{parts[0]}.{parts[1]}",
            AzureAdWebApplicationFactory.SigningKey
        );
        if (
            !CryptographicOperations.FixedTimeEquals(
                Base64UrlDecode(parts[2]),
                Base64UrlDecode(expectedSignature)
            )
        )
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid signature."));
        }

        var payload = JsonSerializer.Deserialize<JsonElement>(Base64UrlDecode(parts[1]));

        if (payload.GetProperty("iss").GetString() != AzureAdWebApplicationFactory.Issuer)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid issuer."));
        }
        if (payload.GetProperty("aud").GetString() != AzureAdWebApplicationFactory.Audience)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid audience."));
        }
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.GetProperty("exp").GetInt64())
        {
            return Task.FromResult(AuthenticateResult.Fail("Token expired."));
        }

        var identity = new ClaimsIdentity(
            [new Claim("oid", payload.GetProperty("oid").GetString()!)],
            Scheme.Name
        );
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string ComputeSignature(string unsignedToken, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - (padded.Length % 4)) % 4);
        return Convert.FromBase64String(padded);
    }
}
#endif

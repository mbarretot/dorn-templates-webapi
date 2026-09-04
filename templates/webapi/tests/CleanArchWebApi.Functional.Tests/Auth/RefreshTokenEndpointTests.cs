#if (UseCustomAuth)
using System.Text.Json;
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.Domain.Users;

namespace CleanArchWebApi.Functional.Tests.Auth;

public sealed class RefreshTokenEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RefreshTokenEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_RotatesAndReturnsNewPair()
    {
        var login = await LoginAsync();

        var response = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new { RefreshToken = login.RefreshToken }
        );
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var payload = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("accessToken").GetString()));
        var newRefreshToken = payload.GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(newRefreshToken));
        Assert.NotEqual(login.RefreshToken, newRefreshToken);
        Assert.True(payload.TryGetProperty("expiresAt", out _));
        Assert.True(payload.TryGetProperty("refreshTokenExpiresAt", out _));
    }

    [Fact]
    public async Task Refresh_WithReusedRotatedToken_ReturnsUnauthorizedAndRevokesChain()
    {
        var login = await LoginAsync();
        var rotated = await RefreshAsync(login.RefreshToken);
        Assert.True(rotated.Success, rotated.Body);

        // Replay the original (now-rotated-away) token: this is the reuse-of-stolen-token case.
        var replay = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new { RefreshToken = login.RefreshToken }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // The whole chain is revoked as a compromise signal, so even the legitimately-rotated
        // token issued moments ago must now be rejected too.
        var secondAttempt = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new { RefreshToken = rotated.RefreshToken }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        const string rawToken = "expired-raw-refresh-token-value";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = dbContext.Users.Single(u => u.Email == AuthWebApplicationFactory.DemoEmail);
            dbContext.RefreshTokens.Add(
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TokenHash = RefreshTokenHasher.Hash(rawToken),
                    ExpiresAt = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                }
            );
            dbContext.SaveChanges();
        }

        var response = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new { RefreshToken = rawToken }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new { RefreshToken = "not-a-real-token" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(string AccessToken, string RefreshToken)> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { Email = AuthWebApplicationFactory.DemoEmail, Password = _factory.DemoPassword }
        );
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (
            payload.GetProperty("accessToken").GetString()!,
            payload.GetProperty("refreshToken").GetString()!
        );
    }

    private async Task<(bool Success, string RefreshToken, string Body)> RefreshAsync(
        string refreshToken
    )
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new { RefreshToken = refreshToken }
        );
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return (false, string.Empty, body);
        }
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        return (true, payload.GetProperty("refreshToken").GetString()!, body);
    }
}
#endif

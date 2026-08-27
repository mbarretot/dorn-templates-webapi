#if (UseCustomAuth)
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CleanArchWebApi.Functional.Tests.Auth;

public sealed class MeEndpointsTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MeEndpointsTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_WithoutCredentials_ReturnsGenericUnauthorizedWithoutBody()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetMe_WithSeededDemoToken_ReturnsClaims()
    {
        var token = await LoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await _client.GetAsync("/api/me");
        var claims = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(claims);
        Assert.Contains(
            claims!,
            claim => claim.GetProperty("value").GetString() == AuthWebApplicationFactory.DemoEmail
        );
    }

    [Fact]
    public async Task GetMe_WithForeignSigningKey_ReturnsUnauthorizedInsteadOfServerError()
    {
        var token = CreateToken("foreign-signing-key-32-bytes-long-12345");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { Email = AuthWebApplicationFactory.DemoEmail, Password = _factory.DemoPassword }
        );
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("accessToken").GetString()!;
    }

    private static string CreateToken(string signingKey)
    {
        var header = Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" })
        );
        var payload = Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    iss = AuthWebApplicationFactory.Issuer,
                    aud = AuthWebApplicationFactory.Audience,
                    sub = Guid.NewGuid().ToString(),
                    email = AuthWebApplicationFactory.DemoEmail,
                    exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
                }
            )
        );
        var unsignedToken = $"{header}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
        return $"{unsignedToken}.{signature}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
#endif
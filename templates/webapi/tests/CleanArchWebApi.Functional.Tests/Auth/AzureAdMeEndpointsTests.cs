#if (UseAzureAdAuth)
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CleanArchWebApi.Functional.Tests.Auth;

public sealed class AzureAdMeEndpointsTests : IClassFixture<AzureAdWebApplicationFactory>
{
    private const string ObjectId = "azure-ad-test-object-id";

    private readonly HttpClient _client;

    public AzureAdMeEndpointsTests(AzureAdWebApplicationFactory factory)
    {
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
    public async Task GetMe_WithLocallySignedToken_ReturnsClaims()
    {
        var token = CreateToken(AzureAdWebApplicationFactory.SigningKey, ObjectId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await _client.GetAsync("/api/me");
        var claims = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(claims);
        Assert.Contains(claims!, claim => claim.GetProperty("value").GetString() == ObjectId);
    }

    [Fact]
    public async Task GetMe_WithForeignSigningKey_ReturnsUnauthorizedInsteadOfServerError()
    {
        var token = CreateToken("foreign-azure-ad-signing-key-32-bytes-99", ObjectId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithWrongAudience_ReturnsUnauthorized()
    {
        var token = CreateToken(
            AzureAdWebApplicationFactory.SigningKey,
            ObjectId,
            audience: "some-other-apps-client-id"
        );
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateToken(string signingKey, string objectId, string? audience = null)
    {
        var header = Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" })
        );
        var payload = Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    iss = AzureAdWebApplicationFactory.Issuer,
                    aud = audience ?? AzureAdWebApplicationFactory.Audience,
                    sub = Guid.NewGuid().ToString(),
                    oid = objectId,
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

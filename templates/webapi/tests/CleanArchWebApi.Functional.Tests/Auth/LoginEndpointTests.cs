#if (UseCustomAuth)
using System.Text;
using System.Text.Json;

namespace CleanArchWebApi.Functional.Tests.Auth;

public sealed class LoginEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoginEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithSeededDemoCredentials_ReturnsJwt()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { Email = AuthWebApplicationFactory.DemoEmail, Password = _factory.DemoPassword }
        );
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
        var payload = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("accessToken").GetString()));
        Assert.True(payload.TryGetProperty("expiresAt", out _));
    }

    [Fact]
    public async Task Login_WithUnknownEmailAndBadPassword_ReturnsIdenticalUnauthorizedResponses()
    {
        var unknownEmailResponse = await _client.PostAsJsonAsync(
            "/auth/login",
            new { Email = "unknown@example.com", Password = _factory.DemoPassword }
        );
        var badPasswordResponse = await _client.PostAsJsonAsync(
            "/auth/login",
            new { Email = AuthWebApplicationFactory.DemoEmail, Password = "wrong-password-for-test" }
        );
        var unknownEmailBody = await unknownEmailResponse.Content.ReadAsStringAsync();
        var badPasswordBody = await badPasswordResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, badPasswordResponse.StatusCode);
        Assert.Equal(unknownEmailBody, badPasswordBody);
    }

    [Fact]
    public async Task Login_WithMalformedJson_ReturnsBadRequest()
    {
        using var content = new StringContent(
            "{\"email\": \"" + AuthWebApplicationFactory.DemoEmail + "\",",
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
#endif
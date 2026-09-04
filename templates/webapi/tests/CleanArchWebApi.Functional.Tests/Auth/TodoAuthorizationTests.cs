#if (UseCustomAuth)
using System.Net.Http.Headers;
using System.Text.Json;

namespace CleanArchWebApi.Functional.Tests.Auth;

/// <summary>Proves the Todo endpoints enforce the "todos:read"/"todos:write"/"todos:delete" permission policies.</summary>
public sealed class TodoAuthorizationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TodoAuthorizationTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTodos_WithoutCredentials_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_WithoutCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { Title = "test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTodos_WithReadPermission_ReturnsOk()
    {
        await AuthenticateAsAsync(AuthWebApplicationFactory.DemoEmail, _factory.DemoPassword);

        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTodos_WithOnlyReadPermission_ReturnsOk()
    {
        await AuthenticateAsAsync(AuthWebApplicationFactory.LimitedEmail, _factory.LimitedPassword);

        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_WithoutWritePermission_ReturnsForbidden()
    {
        await AuthenticateAsAsync(AuthWebApplicationFactory.LimitedEmail, _factory.LimitedPassword);

        var response = await _client.PostAsJsonAsync("/api/todos", new { Title = "test" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_WithWritePermission_Succeeds()
    {
        await AuthenticateAsAsync(AuthWebApplicationFactory.DemoEmail, _factory.DemoPassword);

        var response = await _client.PostAsJsonAsync("/api/todos", new { Title = "test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_WithoutDeletePermission_ReturnsForbidden()
    {
        await AuthenticateAsAsync(AuthWebApplicationFactory.LimitedEmail, _factory.LimitedPassword);

        var response = await _client.DeleteAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_WithDeletePermission_ReturnsNotFoundInsteadOfForbidden()
    {
        await AuthenticateAsAsync(AuthWebApplicationFactory.DemoEmail, _factory.DemoPassword);

        var response = await _client.DeleteAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task AuthenticateAsAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { Email = email, Password = password }
        );
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            payload.GetProperty("accessToken").GetString()
        );
    }
}
#endif

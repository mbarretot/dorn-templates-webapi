using CleanArchWebApi.WebApi.Extensions;

namespace CleanArchWebApi.Functional.Tests;

/// <summary>Proves the global fixed-window limiter is actually wired into the real HTTP pipeline.</summary>
public sealed class RateLimitingTests : IClassFixture<TodoWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(TodoWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Requests_BeyondTheConfiguredLimit_ReturnTooManyRequests()
    {
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < RateLimitingExtensions.PermitLimit; i++)
        {
            lastResponse = await _client.GetAsync("/api/todos");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        }

        lastResponse = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }
}

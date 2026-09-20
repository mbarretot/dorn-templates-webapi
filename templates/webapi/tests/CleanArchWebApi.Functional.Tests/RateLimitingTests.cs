using CleanArchWebApi.WebApi.Extensions;

namespace CleanArchWebApi.Functional.Tests;

/// <summary>Proves the global fixed-window limiter is actually wired into the real HTTP pipeline.</summary>
public sealed class RateLimitingTests : IClassFixture<ApiWebApplicationFactory>
{
#if (IncludeSample)
    private const string ProbePath = "/api/todos";
#else
    private const string ProbePath = "/openapi/v1.json";
#endif

    private readonly HttpClient _client;

    public RateLimitingTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Requests_BeyondTheConfiguredLimit_ReturnTooManyRequests()
    {
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < RateLimitingExtensions.PermitLimit; i++)
        {
            lastResponse = await _client.GetAsync(ProbePath);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        }

        lastResponse = await _client.GetAsync(ProbePath);

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }
}

#if (!UseAspire)
namespace CleanArchWebApi.Functional.Tests;

/// <summary>
/// Aspire's ServiceDefaults maps its own /health endpoint (dev-only); every other orchestrator
/// relies on the baseline one wired up directly in Program.cs.
/// </summary>
public sealed class HealthCheckEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
#endif

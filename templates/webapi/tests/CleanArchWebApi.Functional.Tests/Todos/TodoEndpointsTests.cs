namespace CleanArchWebApi.Functional.Tests.Todos;

/// <summary>Round-trips the real Minimal API endpoints over an in-memory TestServer.</summary>
public sealed class TodoEndpointsTests : IClassFixture<TodoWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TodoEndpointsTests(TodoWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostThenGet_TodoItem_RoundTripsThroughTheRealHttpPipeline()
    {
        var postResponse = await _client.PostAsJsonAsync(
            "/api/todos",
            new { Title = "Ship the four-tier test strategy" }
        );

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var createdId = await postResponse.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, createdId);

        var getResponse = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var items = await getResponse.Content.ReadFromJsonAsync<List<TodoItemDto>>();
        Assert.NotNull(items);
        Assert.Contains(
            items!,
            item => item.Id == createdId && item.Title == "Ship the four-tier test strategy"
        );
    }
}

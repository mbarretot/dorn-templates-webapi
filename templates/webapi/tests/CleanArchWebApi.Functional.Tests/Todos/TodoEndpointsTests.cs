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

    [Fact]
    public async Task GetById_WhenItemExists_ReturnsIt()
    {
        var createdId = await CreateTodoItemAsync("Prove GetById works");

        var response = await _client.GetAsync($"/api/todos/{createdId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.NotNull(item);
        Assert.Equal(createdId, item!.Id);
        Assert.Equal("Prove GetById works", item.Title);
    }

    [Fact]
    public async Task GetById_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTheTitle()
    {
        var createdId = await CreateTodoItemAsync("Original title");

        var putResponse = await _client.PutAsJsonAsync(
            $"/api/todos/{createdId}",
            new { Title = "Renamed title" }
        );

        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);
        var getResponse = await _client.GetAsync($"/api/todos/{createdId}");
        var item = await getResponse.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.Equal("Renamed title", item!.Title);
    }

    [Fact]
    public async Task Put_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/todos/{Guid.NewGuid()}",
            new { Title = "Doesn't matter" }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchComplete_MarksTheItemComplete()
    {
        var createdId = await CreateTodoItemAsync("Mark me complete");

        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/todos/{createdId}/complete",
            new { IsComplete = true }
        );

        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);
        var getResponse = await _client.GetAsync($"/api/todos/{createdId}");
        var item = await getResponse.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.True(item!.IsComplete);
    }

    [Fact]
    public async Task Delete_RemovesTheItem()
    {
        var createdId = await CreateTodoItemAsync("Delete me");

        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var getResponse = await _client.GetAsync($"/api/todos/{createdId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateTodoItemAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { Title = title });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}

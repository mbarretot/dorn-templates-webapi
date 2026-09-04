namespace CleanArchWebApi.Functional.Tests.Todos;

/// <summary>Proves HybridCache is wired into the real HTTP pipeline without ever serving stale data
/// across a mutation: every endpoint that reads a cached entry must reflect the latest write.</summary>
public sealed class TodoCachingTests : IClassFixture<TodoWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TodoCachingTests(TodoWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_AfterUpdate_ReturnsFreshData_NotTheCachedValue()
    {
        var createdId = await CreateTodoItemAsync("Original title");
        var cached = await _client.GetFromJsonAsync<TodoItemDto>($"/api/todos/{createdId}");
        Assert.Equal("Original title", cached!.Title);

        var putResponse = await _client.PutAsJsonAsync(
            $"/api/todos/{createdId}",
            new { Title = "Renamed title" }
        );
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var fresh = await _client.GetFromJsonAsync<TodoItemDto>($"/api/todos/{createdId}");

        Assert.Equal("Renamed title", fresh!.Title);
    }

    [Fact]
    public async Task GetById_AfterDelete_NoLongerReturnsTheCachedItem()
    {
        var createdId = await CreateTodoItemAsync("About to be deleted");
        var cached = await _client.GetFromJsonAsync<TodoItemDto>($"/api/todos/{createdId}");
        Assert.NotNull(cached);

        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/todos/{createdId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_AfterCreate_IncludesTheNewItem_NotTheStaleCachedList()
    {
        // Populate the list cache entry before the mutation.
        await _client.GetAsync("/api/todos");

        var createdId = await CreateTodoItemAsync("Freshly created after list was cached");

        var items = await _client.GetFromJsonAsync<List<TodoItemDto>>("/api/todos");

        Assert.Contains(items!, item => item.Id == createdId);
    }

    private async Task<Guid> CreateTodoItemAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { Title = title });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}

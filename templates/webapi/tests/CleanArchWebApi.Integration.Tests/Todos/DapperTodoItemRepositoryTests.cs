#if (UseDapper)
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Events;
using CleanArchWebApi.Infrastructure.Repositories.Dapper;
using Microsoft.Extensions.Configuration;

namespace CleanArchWebApi.Integration.Tests.Todos;

/// <summary>Exercises the Dapper repository's unit-of-work and domain-event publishing against a real SQLite file.</summary>
public sealed class DapperTodoItemRepositoryTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"{Guid.NewGuid()}.db"
    );
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ITodoItemRepository _repository;

    public DapperTodoItemRepositoryTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = $"Data Source={_databasePath}",
                }
            )
            .Build();

        var context = new DapperContext(configuration);
        using (var connection = context.CreateConnection())
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "CREATE TABLE TodoItems (Id TEXT PRIMARY KEY, Title TEXT NOT NULL, IsComplete INTEGER NOT NULL)";
            command.ExecuteNonQuery();
        }

        _repository = new TodoItemRepository(context, _publisher);
    }

    [Fact]
    public async Task Add_ThenSaveChangesAsync_PersistsAndPublishesTheCreatedEvent()
    {
        var todoItem = TodoItem.Create("Prove Dapper persists and publishes events");

        _repository.Add(todoItem);
        await _repository.SaveChangesAsync(CancellationToken.None);

        var reloaded = await _repository.GetByIdAsync(todoItem.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(todoItem.Title, reloaded!.Title);
        Assert.False(reloaded.IsComplete);

        await _publisher
            .Received(1)
            .Publish(
                Arg.Is<TodoItemCreatedEvent>(e => e.TodoItemId == todoItem.Id),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Add_WithoutSaveChangesAsync_DoesNotPersist()
    {
        var todoItem = TodoItem.Create("Never actually saved");

        _repository.Add(todoItem);

        var reloaded = await _repository.GetByIdAsync(todoItem.Id);
        Assert.Null(reloaded);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
#endif

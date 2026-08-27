using CleanArchWebApi.Application.Todos.CreateTodoItem;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class CreateTodoItemCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IPublisher _publisher;
    private readonly ApplicationDbContext _dbContext;

    public CreateTodoItemCommandHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _publisher = Substitute.For<IPublisher>();
        _dbContext = new ApplicationDbContext(options, _publisher);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Handle_ShouldCreateTodoItem_AndReturnItsId()
    {
        var handler = new CreateTodoItemCommandHandler(_dbContext);
        var command = new CreateTodoItemCommand("Write the Dorn scaffolding");

        var id = await handler.Handle(command, CancellationToken.None);

        var createdItem = await _dbContext.Items.FindAsync(id);
        Assert.NotNull(createdItem);
        Assert.Equal("Write the Dorn scaffolding", createdItem!.Title);
        Assert.False(createdItem.IsComplete);
    }

    [Fact]
    public async Task Handle_ShouldPublishTodoItemCreatedEvent_AfterSaving()
    {
        var handler = new CreateTodoItemCommandHandler(_dbContext);
        var command = new CreateTodoItemCommand("Write the Dorn scaffolding");

        var id = await handler.Handle(command, CancellationToken.None);

        await _publisher
            .Received(1)
            .Publish(
                Arg.Is<TodoItemCreatedEvent>(e =>
                    e.TodoItemId == id && e.Title == "Write the Dorn scaffolding"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}

using CleanArchWebApi.Application.Todos.UpdateTodoItem;
using CleanArchWebApi.Domain.Entities;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class UpdateTodoItemCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public UpdateTodoItemCommandHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new ApplicationDbContext(options, Substitute.For<IPublisher>());
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Handle_WhenItemExists_RenamesItAndReturnsTrue()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _dbContext.Items.Add(todoItem);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateTodoItemCommandHandler(_dbContext);

        var result = await handler.Handle(
            new UpdateTodoItemCommand(todoItem.Id, "Ship the release"),
            CancellationToken.None
        );

        Assert.True(result);
        var updated = await _dbContext.Items.FindAsync(todoItem.Id);
        Assert.Equal("Ship the release", updated!.Title);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsFalse()
    {
        var handler = new UpdateTodoItemCommandHandler(_dbContext);

        var result = await handler.Handle(
            new UpdateTodoItemCommand(Guid.NewGuid(), "Ship the release"),
            CancellationToken.None
        );

        Assert.False(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}

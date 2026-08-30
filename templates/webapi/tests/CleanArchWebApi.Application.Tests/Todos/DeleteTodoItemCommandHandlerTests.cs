using CleanArchWebApi.Application.Todos.DeleteTodoItem;
using CleanArchWebApi.Domain.Entities;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class DeleteTodoItemCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public DeleteTodoItemCommandHandlerTests()
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
    public async Task Handle_WhenItemExists_RemovesItAndReturnsTrue()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _dbContext.Items.Add(todoItem);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new DeleteTodoItemCommandHandler(_dbContext);

        var result = await handler.Handle(
            new DeleteTodoItemCommand(todoItem.Id),
            CancellationToken.None
        );

        Assert.True(result);
        Assert.Null(await _dbContext.Items.FindAsync(todoItem.Id));
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsFalse()
    {
        var handler = new DeleteTodoItemCommandHandler(_dbContext);

        var result = await handler.Handle(
            new DeleteTodoItemCommand(Guid.NewGuid()),
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

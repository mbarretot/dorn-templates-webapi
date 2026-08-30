using CleanArchWebApi.Application.Todos.SetTodoItemCompletion;
using CleanArchWebApi.Domain.Entities;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class SetTodoItemCompletionCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public SetTodoItemCompletionCommandHandlerTests()
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
    public async Task Handle_WhenItemExists_SetsIsCompleteAndReturnsTrue()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _dbContext.Items.Add(todoItem);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SetTodoItemCompletionCommandHandler(_dbContext);

        var result = await handler.Handle(
            new SetTodoItemCompletionCommand(todoItem.Id, true),
            CancellationToken.None
        );

        Assert.True(result);
        var updated = await _dbContext.Items.FindAsync(todoItem.Id);
        Assert.True(updated!.IsComplete);
    }

    [Fact]
    public async Task Handle_WhenSettingBackToIncomplete_UpdatesTheFlag()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        todoItem.MarkComplete();
        _dbContext.Items.Add(todoItem);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new SetTodoItemCompletionCommandHandler(_dbContext);

        await handler.Handle(
            new SetTodoItemCompletionCommand(todoItem.Id, false),
            CancellationToken.None
        );

        var updated = await _dbContext.Items.FindAsync(todoItem.Id);
        Assert.False(updated!.IsComplete);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsFalse()
    {
        var handler = new SetTodoItemCompletionCommandHandler(_dbContext);

        var result = await handler.Handle(
            new SetTodoItemCompletionCommand(Guid.NewGuid(), true),
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

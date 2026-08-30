using CleanArchWebApi.Application.Todos.GetTodoItemById;
using CleanArchWebApi.Domain.Entities;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class GetTodoItemByIdQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public GetTodoItemByIdQueryHandlerTests()
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
    public async Task Handle_WhenItemExists_ReturnsDto()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _dbContext.Items.Add(todoItem);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new GetTodoItemByIdQueryHandler(_dbContext);

        var result = await handler.Handle(
            new GetTodoItemByIdQuery(todoItem.Id),
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.Equal(todoItem.Id, result!.Id);
        Assert.Equal(todoItem.Title, result.Title);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsNull()
    {
        var handler = new GetTodoItemByIdQueryHandler(_dbContext);

        var result = await handler.Handle(
            new GetTodoItemByIdQuery(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Null(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}

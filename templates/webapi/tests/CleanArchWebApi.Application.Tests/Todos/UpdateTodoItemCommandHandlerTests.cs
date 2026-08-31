using CleanArchWebApi.Application.Todos.UpdateTodoItem;
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class UpdateTodoItemCommandHandlerTests
{
    private readonly ITodoItemRepository _repository = Substitute.For<ITodoItemRepository>();

    [Fact]
    public async Task Handle_WhenItemExists_RenamesItAndReturnsTrue()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _repository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>()).Returns(todoItem);

        var handler = new UpdateTodoItemCommandHandler(_repository);

        var result = await handler.Handle(
            new UpdateTodoItemCommand(todoItem.Id, "Ship the release"),
            CancellationToken.None
        );

        Assert.True(result);
        Assert.Equal("Ship the release", todoItem.Title);
        _repository.Received(1).Update(todoItem);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsFalse()
    {
        _repository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TodoItem?)null);

        var handler = new UpdateTodoItemCommandHandler(_repository);

        var result = await handler.Handle(
            new UpdateTodoItemCommand(Guid.NewGuid(), "Ship the release"),
            CancellationToken.None
        );

        Assert.False(result);
    }
}

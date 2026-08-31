using CleanArchWebApi.Application.Todos.DeleteTodoItem;
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class DeleteTodoItemCommandHandlerTests
{
    private readonly ITodoItemRepository _repository = Substitute.For<ITodoItemRepository>();

    [Fact]
    public async Task Handle_WhenItemExists_RemovesItAndReturnsTrue()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _repository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>()).Returns(todoItem);

        var handler = new DeleteTodoItemCommandHandler(_repository);

        var result = await handler.Handle(
            new DeleteTodoItemCommand(todoItem.Id),
            CancellationToken.None
        );

        Assert.True(result);
        _repository.Received(1).Remove(todoItem);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsFalse()
    {
        _repository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TodoItem?)null);

        var handler = new DeleteTodoItemCommandHandler(_repository);

        var result = await handler.Handle(
            new DeleteTodoItemCommand(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.False(result);
    }
}

using CleanArchWebApi.Application.Todos.SetTodoItemCompletion;
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class SetTodoItemCompletionCommandHandlerTests
{
    private readonly ITodoItemRepository _repository = Substitute.For<ITodoItemRepository>();

    [Fact]
    public async Task Handle_WhenItemExists_SetsIsCompleteAndReturnsTrue()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _repository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>()).Returns(todoItem);

        var handler = new SetTodoItemCompletionCommandHandler(_repository);

        var result = await handler.Handle(
            new SetTodoItemCompletionCommand(todoItem.Id, true),
            CancellationToken.None
        );

        Assert.True(result);
        Assert.True(todoItem.IsComplete);
        _repository.Received(1).Update(todoItem);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSettingBackToIncomplete_UpdatesTheFlag()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        todoItem.MarkComplete();
        _repository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>()).Returns(todoItem);

        var handler = new SetTodoItemCompletionCommandHandler(_repository);

        await handler.Handle(
            new SetTodoItemCompletionCommand(todoItem.Id, false),
            CancellationToken.None
        );

        Assert.False(todoItem.IsComplete);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsFalse()
    {
        _repository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TodoItem?)null);

        var handler = new SetTodoItemCompletionCommandHandler(_repository);

        var result = await handler.Handle(
            new SetTodoItemCompletionCommand(Guid.NewGuid(), true),
            CancellationToken.None
        );

        Assert.False(result);
    }
}

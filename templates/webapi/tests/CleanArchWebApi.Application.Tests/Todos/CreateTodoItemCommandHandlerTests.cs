using CleanArchWebApi.Application.Todos.CreateTodoItem;
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class CreateTodoItemCommandHandlerTests
{
    private const string Title = "Write the Dorn scaffolding";

    private readonly ITodoItemRepository _repository = Substitute.For<ITodoItemRepository>();

    [Fact]
    public async Task Handle_AddsTodoItemAndSavesChanges()
    {
        var handler = new CreateTodoItemCommandHandler(_repository);
        var command = new CreateTodoItemCommand(Title);

        var id = await handler.Handle(command, CancellationToken.None);

        _repository.Received(1).Add(Arg.Is<TodoItem>(item => item.Id == id && item.Title == Title));
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsTheCreatedTodoItemId()
    {
        var handler = new CreateTodoItemCommandHandler(_repository);
        var command = new CreateTodoItemCommand(Title);

        var id = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
    }
}

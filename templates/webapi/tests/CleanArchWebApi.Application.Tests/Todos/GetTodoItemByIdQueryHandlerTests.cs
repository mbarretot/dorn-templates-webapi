using CleanArchWebApi.Application.Todos.GetTodoItemById;
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class GetTodoItemByIdQueryHandlerTests
{
    private readonly ITodoItemRepository _repository = Substitute.For<ITodoItemRepository>();

    [Fact]
    public async Task Handle_WhenItemExists_ReturnsDto()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");
        _repository.GetByIdAsync(todoItem.Id, Arg.Any<CancellationToken>()).Returns(todoItem);

        var handler = new GetTodoItemByIdQueryHandler(_repository);

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
        _repository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TodoItem?)null);

        var handler = new GetTodoItemByIdQueryHandler(_repository);

        var result = await handler.Handle(
            new GetTodoItemByIdQuery(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Null(result);
    }
}

using CleanArchWebApi.Application.Todos.GetTodoItems;
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class GetTodoItemsQueryHandlerTests
{
    private readonly ITodoItemRepository _repository = Substitute.For<ITodoItemRepository>();

    [Fact]
    public async Task Handle_ReturnsAllItemsFromRepositoryAsDtos()
    {
        var first = TodoItem.Create("Write the Dorn scaffolding");
        var second = TodoItem.Rehydrate(Guid.NewGuid(), "Ship the release", isComplete: true);
        _repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TodoItem> { first, second });

        var handler = new GetTodoItemsQueryHandler(_repository);

        var result = await handler.Handle(new GetTodoItemsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(
            result,
            dto => dto.Id == first.Id && dto.Title == first.Title && !dto.IsComplete
        );
        Assert.Contains(
            result,
            dto => dto.Id == second.Id && dto.Title == second.Title && dto.IsComplete
        );
    }
}

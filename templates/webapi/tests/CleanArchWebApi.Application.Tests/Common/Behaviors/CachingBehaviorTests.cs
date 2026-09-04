using CleanArchWebApi.Application.Common.Behaviors;
using CleanArchWebApi.Application.Todos.GetTodoItems;
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.Application.Tests.Common.Behaviors;

public sealed class CachingBehaviorTests
{
    public sealed record NonCacheableRequest : IRequest<string>;

    private static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task Handle_WhenRequestIsCacheable_CallsNextOnlyOnceForRepeatedCallsWithTheSameKey()
    {
        var behavior = new CachingBehavior<GetTodoItemsQuery, List<TodoItemDto>>(CreateCache());
        var request = new GetTodoItemsQuery();
        var callCount = 0;
        RequestHandlerDelegate<List<TodoItemDto>> next = () =>
        {
            callCount++;
            return Task.FromResult(
                new List<TodoItemDto> { new(Guid.NewGuid(), "Cached item", false) }
            );
        };

        var first = await behavior.Handle(request, next, CancellationToken.None);
        var second = await behavior.Handle(request, next, CancellationToken.None);

        Assert.Equal(1, callCount);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotCacheable_CallsNextEveryTime()
    {
        var behavior = new CachingBehavior<NonCacheableRequest, string>(CreateCache());
        var callCount = 0;
        RequestHandlerDelegate<string> next = () =>
        {
            callCount++;
            return Task.FromResult("result");
        };

        await behavior.Handle(new NonCacheableRequest(), next, CancellationToken.None);
        await behavior.Handle(new NonCacheableRequest(), next, CancellationToken.None);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Handle_ComposedWithTheRealQueryHandler_HitsTheRepositoryOnlyOnce()
    {
        var repository = Substitute.For<ITodoItemRepository>();
        repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TodoItem> { TodoItem.Create("Write the Dorn scaffolding") });
        var handler = new GetTodoItemsQueryHandler(repository);
        var behavior = new CachingBehavior<GetTodoItemsQuery, List<TodoItemDto>>(CreateCache());
        var request = new GetTodoItemsQuery();
        RequestHandlerDelegate<List<TodoItemDto>> next = () =>
            handler.Handle(request, CancellationToken.None);

        await behavior.Handle(request, next, CancellationToken.None);
        await behavior.Handle(request, next, CancellationToken.None);

        await repository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}

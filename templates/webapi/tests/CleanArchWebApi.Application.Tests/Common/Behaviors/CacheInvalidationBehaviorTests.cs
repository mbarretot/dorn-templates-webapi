using CleanArchWebApi.Application.Common.Behaviors;
using CleanArchWebApi.Application.Common.Caching;
using CleanArchWebApi.Application.Todos.CreateTodoItem;
using CleanArchWebApi.Application.Todos.DeleteTodoItem;
using CleanArchWebApi.Application.Todos.SetTodoItemCompletion;
using CleanArchWebApi.Application.Todos.UpdateTodoItem;
using Microsoft.Extensions.Caching.Hybrid;

namespace CleanArchWebApi.Application.Tests.Common.Behaviors;

public sealed class CacheInvalidationBehaviorTests
{
    public sealed record NonInvalidatingRequest : IRequest<string>;

    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    [Fact]
    public async Task Handle_WhenCommandIsNotCacheInvalidating_NeverTouchesTheCache()
    {
        var behavior = new CacheInvalidationBehavior<NonInvalidatingRequest, string>(_cache);
        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        var result = await behavior.Handle(
            new NonInvalidatingRequest(),
            next,
            CancellationToken.None
        );

        Assert.Equal("result", result);
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreateCommand_InvalidatesOnlyTheListKey()
    {
        var behavior = new CacheInvalidationBehavior<CreateTodoItemCommand, Guid>(_cache);
        RequestHandlerDelegate<Guid> next = () => Task.FromResult(Guid.NewGuid());

        await behavior.Handle(new CreateTodoItemCommand("Title"), next, CancellationToken.None);

        await _cache.Received(1).RemoveAsync(TodoCacheKeys.All(), Arg.Any<CancellationToken>());
        await _cache
            .Received(0)
            .RemoveAsync(
                Arg.Is<string>(key => key != TodoCacheKeys.All()),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_UpdateCommand_InvalidatesListAndItemKeys()
    {
        var behavior = new CacheInvalidationBehavior<UpdateTodoItemCommand, bool>(_cache);
        var id = Guid.NewGuid();
        RequestHandlerDelegate<bool> next = () => Task.FromResult(true);

        var result = await behavior.Handle(
            new UpdateTodoItemCommand(id, "Renamed"),
            next,
            CancellationToken.None
        );

        Assert.True(result);
        await _cache.Received(1).RemoveAsync(TodoCacheKeys.All(), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(TodoCacheKeys.ById(id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetCompletionCommand_InvalidatesListAndItemKeys()
    {
        var behavior = new CacheInvalidationBehavior<SetTodoItemCompletionCommand, bool>(_cache);
        var id = Guid.NewGuid();
        RequestHandlerDelegate<bool> next = () => Task.FromResult(true);

        await behavior.Handle(
            new SetTodoItemCompletionCommand(id, true),
            next,
            CancellationToken.None
        );

        await _cache.Received(1).RemoveAsync(TodoCacheKeys.All(), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(TodoCacheKeys.ById(id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeleteCommand_InvalidatesListAndItemKeys()
    {
        var behavior = new CacheInvalidationBehavior<DeleteTodoItemCommand, bool>(_cache);
        var id = Guid.NewGuid();
        RequestHandlerDelegate<bool> next = () => Task.FromResult(true);

        await behavior.Handle(new DeleteTodoItemCommand(id), next, CancellationToken.None);

        await _cache.Received(1).RemoveAsync(TodoCacheKeys.All(), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(TodoCacheKeys.ById(id), Arg.Any<CancellationToken>());
    }
}

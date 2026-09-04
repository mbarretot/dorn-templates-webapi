namespace CleanArchWebApi.Application.Todos.CreateTodoItem;

public sealed record CreateTodoItemCommand(string Title) : IRequest<Guid>, ICacheInvalidatingCommand
{
    // No prior item cache entry to invalidate: this Id doesn't exist until the handler creates it.
    public IReadOnlyCollection<string> CacheKeysToInvalidate => new[] { TodoCacheKeys.All() };
}

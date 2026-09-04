namespace CleanArchWebApi.Application.Todos.DeleteTodoItem;

public sealed record DeleteTodoItemCommand(Guid Id) : IRequest<bool>, ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CacheKeysToInvalidate =>
        new[] { TodoCacheKeys.All(), TodoCacheKeys.ById(Id) };
}

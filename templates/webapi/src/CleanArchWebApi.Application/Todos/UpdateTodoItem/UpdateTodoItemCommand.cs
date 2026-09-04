namespace CleanArchWebApi.Application.Todos.UpdateTodoItem;

public sealed record UpdateTodoItemCommand(Guid Id, string Title)
    : IRequest<bool>,
        ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CacheKeysToInvalidate =>
        new[] { TodoCacheKeys.All(), TodoCacheKeys.ById(Id) };
}

namespace CleanArchWebApi.Application.Todos.SetTodoItemCompletion;

public sealed record SetTodoItemCompletionCommand(Guid Id, bool IsComplete)
    : IRequest<bool>,
        ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CacheKeysToInvalidate =>
        new[] { TodoCacheKeys.All(), TodoCacheKeys.ById(Id) };
}

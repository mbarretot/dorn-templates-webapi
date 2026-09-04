namespace CleanArchWebApi.Application.Todos.GetTodoItems;

public sealed record GetTodoItemsQuery
    : IRequest<List<TodoItemDto>>,
        ICacheableQuery<List<TodoItemDto>>
{
    public string CacheKey => TodoCacheKeys.All();
}

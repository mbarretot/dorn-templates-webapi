namespace CleanArchWebApi.Application.Todos.GetTodoItems;

public sealed class GetTodoItemsQueryHandler : IRequestHandler<GetTodoItemsQuery, List<TodoItemDto>>
{
    private readonly ITodoItemRepository _repository;

    public GetTodoItemsQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TodoItemDto>> Handle(GetTodoItemsQuery request, CancellationToken ct)
    {
        var items = await _repository.GetAllAsync(ct);

        return items.Select(item => new TodoItemDto(item.Id, item.Title, item.IsComplete)).ToList();
    }
}

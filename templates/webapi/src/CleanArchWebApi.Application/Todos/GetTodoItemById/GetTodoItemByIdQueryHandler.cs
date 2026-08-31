using CleanArchWebApi.Application.Todos.GetTodoItems;

namespace CleanArchWebApi.Application.Todos.GetTodoItemById;

public sealed class GetTodoItemByIdQueryHandler
    : IRequestHandler<GetTodoItemByIdQuery, TodoItemDto?>
{
    private readonly ITodoItemRepository _repository;

    public GetTodoItemByIdQueryHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoItemDto?> Handle(GetTodoItemByIdQuery request, CancellationToken ct)
    {
        var todoItem = await _repository.GetByIdAsync(request.Id, ct);

        return todoItem is null
            ? null
            : new TodoItemDto(todoItem.Id, todoItem.Title, todoItem.IsComplete);
    }
}

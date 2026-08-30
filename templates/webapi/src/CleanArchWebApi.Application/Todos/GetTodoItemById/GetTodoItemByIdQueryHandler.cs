using CleanArchWebApi.Application.Todos.GetTodoItems;

namespace CleanArchWebApi.Application.Todos.GetTodoItemById;

public sealed class GetTodoItemByIdQueryHandler
    : IRequestHandler<GetTodoItemByIdQuery, TodoItemDto?>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTodoItemByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TodoItemDto?> Handle(GetTodoItemByIdQuery request, CancellationToken ct)
    {
        return await _dbContext
            .Items.Where(item => item.Id == request.Id)
            .Select(item => new TodoItemDto(item.Id, item.Title, item.IsComplete))
            .FirstOrDefaultAsync(ct);
    }
}

namespace CleanArchWebApi.Application.Todos.GetTodoItems;

public sealed class GetTodoItemsQueryHandler : IRequestHandler<GetTodoItemsQuery, List<TodoItemDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetTodoItemsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TodoItemDto>> Handle(GetTodoItemsQuery request, CancellationToken ct)
    {
        return await _dbContext
            .Items.Select(item => new TodoItemDto(item.Id, item.Title, item.IsComplete))
            .ToListAsync(ct);
    }
}

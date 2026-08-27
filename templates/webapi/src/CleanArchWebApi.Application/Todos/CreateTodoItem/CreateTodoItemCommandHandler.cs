namespace CleanArchWebApi.Application.Todos.CreateTodoItem;

public sealed class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateTodoItemCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateTodoItemCommand request, CancellationToken ct)
    {
        var todoItem = TodoItem.Create(request.Title);

        _dbContext.Items.Add(todoItem);
        await _dbContext.SaveChangesAsync(ct);

        return todoItem.Id;
    }
}

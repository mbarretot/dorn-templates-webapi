namespace CleanArchWebApi.Application.Todos.UpdateTodoItem;

public sealed class UpdateTodoItemCommandHandler : IRequestHandler<UpdateTodoItemCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateTodoItemCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateTodoItemCommand request, CancellationToken ct)
    {
        var todoItem = await _dbContext.Items.FindAsync([request.Id], ct);
        if (todoItem is null)
        {
            return false;
        }

        todoItem.Rename(request.Title);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }
}

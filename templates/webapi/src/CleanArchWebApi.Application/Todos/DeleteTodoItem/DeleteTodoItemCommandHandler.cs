namespace CleanArchWebApi.Application.Todos.DeleteTodoItem;

public sealed class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteTodoItemCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteTodoItemCommand request, CancellationToken ct)
    {
        var todoItem = await _dbContext.Items.FindAsync([request.Id], ct);
        if (todoItem is null)
        {
            return false;
        }

        _dbContext.Items.Remove(todoItem);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }
}

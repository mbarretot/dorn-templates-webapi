namespace CleanArchWebApi.Application.Todos.SetTodoItemCompletion;

public sealed class SetTodoItemCompletionCommandHandler
    : IRequestHandler<SetTodoItemCompletionCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;

    public SetTodoItemCompletionCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(SetTodoItemCompletionCommand request, CancellationToken ct)
    {
        var todoItem = await _dbContext.Items.FindAsync([request.Id], ct);
        if (todoItem is null)
        {
            return false;
        }

        if (request.IsComplete)
        {
            todoItem.MarkComplete();
        }
        else
        {
            todoItem.MarkIncomplete();
        }

        await _dbContext.SaveChangesAsync(ct);

        return true;
    }
}

namespace CleanArchWebApi.Application.Todos.SetTodoItemCompletion;

public sealed class SetTodoItemCompletionCommandHandler
    : IRequestHandler<SetTodoItemCompletionCommand, bool>
{
    private readonly ITodoItemRepository _repository;

    public SetTodoItemCompletionCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(SetTodoItemCompletionCommand request, CancellationToken ct)
    {
        var todoItem = await _repository.GetByIdAsync(request.Id, ct);
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

        _repository.Update(todoItem);
        await _repository.SaveChangesAsync(ct);

        return true;
    }
}

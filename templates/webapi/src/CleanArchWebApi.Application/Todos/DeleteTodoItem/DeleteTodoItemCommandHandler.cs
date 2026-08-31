namespace CleanArchWebApi.Application.Todos.DeleteTodoItem;

public sealed class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, bool>
{
    private readonly ITodoItemRepository _repository;

    public DeleteTodoItemCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteTodoItemCommand request, CancellationToken ct)
    {
        var todoItem = await _repository.GetByIdAsync(request.Id, ct);
        if (todoItem is null)
        {
            return false;
        }

        _repository.Remove(todoItem);
        await _repository.SaveChangesAsync(ct);

        return true;
    }
}

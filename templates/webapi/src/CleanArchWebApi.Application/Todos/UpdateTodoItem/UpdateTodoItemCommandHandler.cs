namespace CleanArchWebApi.Application.Todos.UpdateTodoItem;

public sealed class UpdateTodoItemCommandHandler : IRequestHandler<UpdateTodoItemCommand, bool>
{
    private readonly ITodoItemRepository _repository;

    public UpdateTodoItemCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateTodoItemCommand request, CancellationToken ct)
    {
        var todoItem = await _repository.GetByIdAsync(request.Id, ct);
        if (todoItem is null)
        {
            return false;
        }

        todoItem.Rename(request.Title);
        _repository.Update(todoItem);
        await _repository.SaveChangesAsync(ct);

        return true;
    }
}

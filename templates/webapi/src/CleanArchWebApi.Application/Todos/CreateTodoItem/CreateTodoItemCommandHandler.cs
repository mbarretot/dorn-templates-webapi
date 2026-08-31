namespace CleanArchWebApi.Application.Todos.CreateTodoItem;

public sealed class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, Guid>
{
    private readonly ITodoItemRepository _repository;

    public CreateTodoItemCommandHandler(ITodoItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateTodoItemCommand request, CancellationToken ct)
    {
        var todoItem = TodoItem.Create(request.Title);

        _repository.Add(todoItem);
        await _repository.SaveChangesAsync(ct);

        return todoItem.Id;
    }
}

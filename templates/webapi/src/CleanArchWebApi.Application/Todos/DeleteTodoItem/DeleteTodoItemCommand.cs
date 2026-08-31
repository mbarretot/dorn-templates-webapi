namespace CleanArchWebApi.Application.Todos.DeleteTodoItem;

public sealed record DeleteTodoItemCommand(Guid Id) : IRequest<bool>;

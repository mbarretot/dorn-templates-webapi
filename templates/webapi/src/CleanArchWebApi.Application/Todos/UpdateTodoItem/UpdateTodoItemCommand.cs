namespace CleanArchWebApi.Application.Todos.UpdateTodoItem;

public sealed record UpdateTodoItemCommand(Guid Id, string Title) : IRequest<bool>;

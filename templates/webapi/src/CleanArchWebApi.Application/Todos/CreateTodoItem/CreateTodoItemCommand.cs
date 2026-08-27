namespace CleanArchWebApi.Application.Todos.CreateTodoItem;

public sealed record CreateTodoItemCommand(string Title) : IRequest<Guid>;

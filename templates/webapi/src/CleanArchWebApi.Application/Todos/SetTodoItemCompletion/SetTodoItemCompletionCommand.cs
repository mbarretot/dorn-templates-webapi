namespace CleanArchWebApi.Application.Todos.SetTodoItemCompletion;

public sealed record SetTodoItemCompletionCommand(Guid Id, bool IsComplete) : IRequest<bool>;

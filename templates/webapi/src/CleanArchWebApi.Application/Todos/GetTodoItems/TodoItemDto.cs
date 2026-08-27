namespace CleanArchWebApi.Application.Todos.GetTodoItems;

public sealed record TodoItemDto(Guid Id, string Title, bool IsComplete);

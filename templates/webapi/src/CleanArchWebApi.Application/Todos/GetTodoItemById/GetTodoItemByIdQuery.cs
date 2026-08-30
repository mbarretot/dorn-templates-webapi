using CleanArchWebApi.Application.Todos.GetTodoItems;

namespace CleanArchWebApi.Application.Todos.GetTodoItemById;

public sealed record GetTodoItemByIdQuery(Guid Id) : IRequest<TodoItemDto?>;

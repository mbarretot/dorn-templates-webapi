using CleanArchWebApi.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CleanArchWebApi.Application.Todos.CreateTodoItem;

public sealed class TodoItemCreatedEventHandler : INotificationHandler<TodoItemCreatedEvent>
{
    private readonly ILogger<TodoItemCreatedEventHandler> _logger;

    public TodoItemCreatedEventHandler(ILogger<TodoItemCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TodoItemCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Todo item {TodoItemId} created: {Title}",
            notification.TodoItemId,
            notification.Title
        );
        return Task.CompletedTask;
    }
}

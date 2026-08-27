using CleanArchWebApi.Domain.Events;

namespace CleanArchWebApi.Domain.Entities;

public class TodoItem : AggregateRoot
{
    public string Title { get; private set; } = string.Empty;

    public bool IsComplete { get; private set; }

    private TodoItem() { }

    public static TodoItem Create(string title)
    {
        var todoItem = new TodoItem { Title = title };
        todoItem.AddDomainEvent(new TodoItemCreatedEvent(todoItem.Id, todoItem.Title));
        return todoItem;
    }
}

using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class TodoItemTests
{
    [Fact]
    public void Create_RaisesTodoItemCreatedEvent()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");

        var domainEvent = Assert.Single(todoItem.DomainEvents);
        var createdEvent = Assert.IsType<TodoItemCreatedEvent>(domainEvent);
        Assert.Equal(todoItem.Id, createdEvent.TodoItemId);
        Assert.Equal("Write the Dorn scaffolding", createdEvent.Title);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllPendingEvents()
    {
        var todoItem = TodoItem.Create("Write the Dorn scaffolding");

        todoItem.ClearDomainEvents();

        Assert.Empty(todoItem.DomainEvents);
    }
}

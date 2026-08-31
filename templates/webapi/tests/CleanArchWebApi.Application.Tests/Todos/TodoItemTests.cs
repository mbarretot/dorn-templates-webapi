using CleanArchWebApi.Domain.Entities;

namespace CleanArchWebApi.Application.Tests.Todos;

public sealed class TodoItemTests
{
    private const string Title = "Write the Dorn scaffolding";

    [Fact]
    public void Create_RaisesTodoItemCreatedEvent()
    {
        var todoItem = TodoItem.Create(Title);

        var domainEvent = Assert.Single(todoItem.DomainEvents);
        var createdEvent = Assert.IsType<TodoItemCreatedEvent>(domainEvent);
        Assert.Equal(todoItem.Id, createdEvent.TodoItemId);
        Assert.Equal(Title, createdEvent.Title);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllPendingEvents()
    {
        var todoItem = TodoItem.Create(Title);

        todoItem.ClearDomainEvents();

        Assert.Empty(todoItem.DomainEvents);
    }

    [Fact]
    public void Rehydrate_PreservesPersistedIdAndCompletionState()
    {
        var id = Guid.NewGuid();

        var todoItem = TodoItem.Rehydrate(id, Title, isComplete: true);

        Assert.Equal(id, todoItem.Id);
        Assert.Equal(Title, todoItem.Title);
        Assert.True(todoItem.IsComplete);
    }

    [Fact]
    public void Rehydrate_DoesNotRaiseDomainEvents()
    {
        var todoItem = TodoItem.Rehydrate(Guid.NewGuid(), Title, isComplete: false);

        Assert.Empty(todoItem.DomainEvents);
    }
}

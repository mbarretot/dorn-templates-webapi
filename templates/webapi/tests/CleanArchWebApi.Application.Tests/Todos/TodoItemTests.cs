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
    public void Rename_UpdatesTitle()
    {
        var todoItem = TodoItem.Create(Title);

        todoItem.Rename("Ship the release");

        Assert.Equal("Ship the release", todoItem.Title);
    }

    [Fact]
    public void MarkComplete_SetsIsCompleteTrue()
    {
        var todoItem = TodoItem.Create(Title);

        todoItem.MarkComplete();

        Assert.True(todoItem.IsComplete);
    }

    [Fact]
    public void MarkIncomplete_SetsIsCompleteFalse()
    {
        var todoItem = TodoItem.Create(Title);
        todoItem.MarkComplete();

        todoItem.MarkIncomplete();

        Assert.False(todoItem.IsComplete);
    }
}

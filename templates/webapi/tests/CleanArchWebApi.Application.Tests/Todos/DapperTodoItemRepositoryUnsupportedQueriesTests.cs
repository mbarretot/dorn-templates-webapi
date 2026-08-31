#if (UseDapper)
using CleanArchWebApi.Domain.Entities;
using CleanArchWebApi.Infrastructure.Repositories.Dapper;

namespace CleanArchWebApi.Application.Tests.Todos;

/// <summary>
/// Locks in the documented contract for Dapper's expression-based query members: they throw
/// rather than silently returning wrong results, since Dapper has no LINQ provider to translate
/// an arbitrary Expression&lt;Func&lt;TodoItem, bool&gt;&gt; into SQL.
/// </summary>
public sealed class DapperTodoItemRepositoryUnsupportedQueriesTests
{
    // FindAsync/AnyAsync/CountAsync never touch the connection or the publisher before
    // throwing, so the repository doesn't need real collaborators to exercise this behavior.
    private readonly TodoItemRepository _repository = new(context: null!, publisher: null!);

    [Fact]
    public async Task FindAsync_ThrowsNotSupportedExceptionWithGuidance()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            _repository.FindAsync(item => item.IsComplete)
        );

        Assert.Contains("Extend ITodoItemRepository", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnyAsync_ThrowsNotSupportedExceptionWithGuidance()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            _repository.AnyAsync(item => item.IsComplete)
        );

        Assert.Contains("Extend ITodoItemRepository", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CountAsync_ThrowsNotSupportedExceptionWithGuidance()
    {
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            _repository.CountAsync(item => item.IsComplete)
        );

        Assert.Contains("Extend ITodoItemRepository", exception.Message, StringComparison.Ordinal);
    }
}
#endif

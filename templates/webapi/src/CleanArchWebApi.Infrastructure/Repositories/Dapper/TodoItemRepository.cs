using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;
using Dapper;

namespace CleanArchWebApi.Infrastructure.Repositories.Dapper;

public class TodoItemRepository : ITodoItemRepository
{
    private readonly DapperContext _context;

    public TodoItemRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<TodoItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT Id, Title, IsComplete FROM TodoItems WHERE Id = @Id";
        var result = await connection.QueryFirstOrDefaultAsync<TodoItemRow>(
            sql,
            new { Id = id.ToString() }
        );
        return result?.ToEntity();
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT Id, Title, IsComplete FROM TodoItems";
        var results = await connection.QueryAsync<TodoItemRow>(sql);
        return results.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<TodoItem>> FindAsync(
        System.Linq.Expressions.Expression<Func<TodoItem, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException(
            "Expression-based queries require manual SQL translation with Dapper. "
                + "Extend ITodoItemRepository with a custom method for complex queries."
        );
    }

    public async Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<TodoItem, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException(
            "Expression-based queries require manual SQL translation with Dapper. "
                + "Extend ITodoItemRepository with a custom method for complex queries."
        );
    }

    public async Task<int> CountAsync(
        System.Linq.Expressions.Expression<Func<TodoItem, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException(
            "Expression-based queries require manual SQL translation with Dapper. "
                + "Extend ITodoItemRepository with a custom method for complex queries."
        );
    }

    public void Add(TodoItem entity)
    {
        using var connection = _context.CreateConnection();
        connection.Execute(
            "INSERT INTO TodoItems (Id, Title, IsComplete) VALUES (@Id, @Title, @IsComplete)",
            new
            {
                Id = entity.Id.ToString(),
                entity.Title,
                IsComplete = entity.IsComplete,
            }
        );
    }

    public void Update(TodoItem entity)
    {
        using var connection = _context.CreateConnection();
        connection.Execute(
            "UPDATE TodoItems SET Title = @Title, IsComplete = @IsComplete WHERE Id = @Id",
            new
            {
                Id = entity.Id.ToString(),
                entity.Title,
                IsComplete = entity.IsComplete,
            }
        );
    }

    public void Remove(TodoItem entity)
    {
        using var connection = _context.CreateConnection();
        connection.Execute(
            "DELETE FROM TodoItems WHERE Id = @Id",
            new { Id = entity.Id.ToString() }
        );
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private class TodoItemRow
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsComplete { get; set; }

        public TodoItem ToEntity()
        {
            return TodoItem.Create(Title);
        }
    }
}

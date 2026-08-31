using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;
using Dapper;

namespace CleanArchWebApi.Infrastructure.Repositories.Dapper;

public class TodoItemRepository : ITodoItemRepository
{
    private readonly DapperContext _context;
    private readonly IPublisher _publisher;
    private readonly List<TodoItem> _added = [];
    private readonly List<TodoItem> _updated = [];
    private readonly List<TodoItem> _removed = [];

    public TodoItemRepository(DapperContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
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

    // Deferred to SaveChangesAsync, matching EF Core's unit-of-work semantics (and the
    // domain-event publishing that ApplicationDbContext.SaveChangesAsync already relies on).
    public void Add(TodoItem entity) => _added.Add(entity);

    public void Update(TodoItem entity) => _updated.Add(entity);

    public void Remove(TodoItem entity) => _removed.Add(entity);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_added.Count == 0 && _updated.Count == 0 && _removed.Count == 0)
        {
            return;
        }

        using (var connection = _context.CreateConnection())
        {
            foreach (var entity in _added)
            {
                connection.Execute(
                    "INSERT INTO TodoItems (Id, Title, IsComplete) VALUES (@Id, @Title, @IsComplete)",
                    new
                    {
                        Id = entity.Id.ToString(),
                        entity.Title,
                        entity.IsComplete,
                    }
                );
            }

            foreach (var entity in _updated)
            {
                connection.Execute(
                    "UPDATE TodoItems SET Title = @Title, IsComplete = @IsComplete WHERE Id = @Id",
                    new
                    {
                        Id = entity.Id.ToString(),
                        entity.Title,
                        entity.IsComplete,
                    }
                );
            }

            foreach (var entity in _removed)
            {
                connection.Execute(
                    "DELETE FROM TodoItems WHERE Id = @Id",
                    new { Id = entity.Id.ToString() }
                );
            }
        }

        var entitiesWithEvents = _added
            .Concat(_updated)
            .Concat(_removed)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();
        _added.Clear();
        _updated.Clear();
        _removed.Clear();

        foreach (var entity in entitiesWithEvents)
        {
            var domainEvents = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }

    private class TodoItemRow
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsComplete { get; set; }

        public TodoItem ToEntity()
        {
            return TodoItem.Rehydrate(Guid.Parse(Id), Title, IsComplete);
        }
    }
}

using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Entities;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Infrastructure.Repositories.EfCore;

public class TodoItemRepository : ITodoItemRepository
{
    private readonly ApplicationDbContext _context;

    public TodoItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TodoItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Items.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Items.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TodoItem>> FindAsync(
        System.Linq.Expressions.Expression<Func<TodoItem, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Items.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<TodoItem, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Items.AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(
        System.Linq.Expressions.Expression<Func<TodoItem, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Items.CountAsync(predicate, cancellationToken);
    }

    public void Add(TodoItem entity)
    {
        _context.Items.Add(entity);
    }

    public void Update(TodoItem entity)
    {
        _context.Items.Update(entity);
    }

    public void Remove(TodoItem entity)
    {
        _context.Items.Remove(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

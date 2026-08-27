namespace CleanArchWebApi.Domain.Common.Interfaces;

public interface IRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : class
{
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

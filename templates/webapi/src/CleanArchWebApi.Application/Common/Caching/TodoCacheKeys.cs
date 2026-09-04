namespace CleanArchWebApi.Application.Common.Caching;

/// <summary>Single source of truth for Todo cache keys, so reads (<see cref="CachingBehavior{TRequest,TResponse}"/>)
/// and invalidations (<see cref="CacheInvalidationBehavior{TRequest,TResponse}"/>) never drift apart.</summary>
public static class TodoCacheKeys
{
    public static string All() => "todos:all";

    public static string ById(Guid id) => $"todos:{id}";
}

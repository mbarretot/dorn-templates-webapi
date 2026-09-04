using Microsoft.Extensions.Caching.Hybrid;

namespace CleanArchWebApi.Application.Common.Behaviors;

/// <summary>After a command that opts in via <see cref="ICacheInvalidatingCommand"/> runs, removes every cache
/// key it reports as stale. Requests that don't implement the marker interface pass through untouched.</summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly HybridCache _cache;

    public CacheInvalidationBehavior(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct
    )
    {
        var response = await next();

        if (request is ICacheInvalidatingCommand invalidatingCommand)
        {
            foreach (var key in invalidatingCommand.CacheKeysToInvalidate)
            {
                await _cache.RemoveAsync(key, ct);
            }
        }

        return response;
    }
}

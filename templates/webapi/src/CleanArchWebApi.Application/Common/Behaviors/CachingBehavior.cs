using Microsoft.Extensions.Caching.Hybrid;

namespace CleanArchWebApi.Application.Common.Behaviors;

/// <summary>Wraps requests that opt in via <see cref="ICacheableQuery{TResponse}"/> with HybridCache's
/// get-or-create semantics; every other request passes straight through to <c>next</c>.</summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly HybridCache _cache;

    public CachingBehavior(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct
    )
    {
        if (request is not ICacheableQuery<TResponse> cacheableQuery)
        {
            return await next();
        }

        return await _cache.GetOrCreateAsync(
            cacheableQuery.CacheKey,
            async _ => await next(),
            cancellationToken: ct
        );
    }
}

namespace CleanArchWebApi.Application.Common.Caching;

/// <summary>Opts a query into <see cref="CachingBehavior{TRequest,TResponse}"/> by exposing the stable cache
/// key its result should be stored and looked up under.</summary>
public interface ICacheableQuery<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
}

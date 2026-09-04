namespace CleanArchWebApi.Application.Common.Caching;

/// <summary>Opts a command into <see cref="CacheInvalidationBehavior{TRequest,TResponse}"/> by exposing every
/// cache key that goes stale once the command has run.</summary>
public interface ICacheInvalidatingCommand
{
    IReadOnlyCollection<string> CacheKeysToInvalidate { get; }
}

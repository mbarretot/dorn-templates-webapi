using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.WebApi.Extensions;

/// <summary>Registers HybridCache with a short default expiration. No IDistributedCache is registered, so this
/// runs L1 (in-process) only -- correct for this template's single-instance default; add a Redis or SQL Server
/// IDistributedCache registration in Infrastructure for multi-instance deployments.</summary>
public static class CachingExtensions
{
    // Todo data changes often (any create/update/complete/delete invalidates it explicitly anyway), so a short
    // expiration just bounds the worst case where invalidation is somehow missed -- 5 minutes keeps that window
    // small without defeating the point of caching short-lived read traffic.
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = DefaultExpiration,
                LocalCacheExpiration = DefaultExpiration,
            };
        });

        return services;
    }
}

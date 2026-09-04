using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.WebApi.Extensions;

/// <summary>Global fixed-window limiter, partitioned per client IP; sane defaults for a demo API.</summary>
public static class RateLimitingExtensions
{
    public const int PermitLimit = 100;
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // The framework default is 503; 429 is the status RFC 6585 actually assigns to rate limiting.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = PermitLimit,
                            Window = Window,
                            QueueLimit = 0,
                        }
                    )
            );
        });

        return services;
    }
}

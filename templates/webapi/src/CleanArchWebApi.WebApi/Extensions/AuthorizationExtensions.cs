#if (UseAuth)
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.WebApi.Extensions;

/// <summary>Registers one named policy per <see cref="Permissions"/> constant (e.g. "todos:read"), all backed by
/// the same <see cref="PermissionAuthorizationHandler"/>. A named-policy loop is simpler to read here than a
/// dynamic IAuthorizationPolicyProvider, and the permission set is small and closed (defined once, in one
/// place) so there is no need to synthesize policies on demand.</summary>
public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(
                    permission,
                    policy => policy.Requirements.Add(new PermissionRequirement(permission))
                );
            }
        });

        return services;
    }
}
#endif

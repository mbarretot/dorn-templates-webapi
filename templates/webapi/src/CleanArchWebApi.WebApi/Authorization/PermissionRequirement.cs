#if (UseAuth)
using Microsoft.AspNetCore.Authorization;

namespace CleanArchWebApi.WebApi.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
#endif

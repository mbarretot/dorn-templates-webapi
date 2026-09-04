#if (UseAuth)
namespace CleanArchWebApi.Application.Common.Security;

/// <summary>
/// Fine-grained permission strings for the Todo endpoints, checked as a claim value (see
/// <see cref="ClaimType"/>) by the authorization policies registered in AuthorizationExtensions.
/// For UseCustomAuth, JwtTokenService issues this claim from AppUser.Permissions. For
/// UseAzureAdAuth, this app has no seeding story -- Entra ID (via App Roles or a claims-mapping
/// policy) must be configured to emit a "permission" claim with these exact values.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "permission";

    public const string TodosRead = "todos:read";
    public const string TodosWrite = "todos:write";
    public const string TodosDelete = "todos:delete";

    public static readonly IReadOnlyList<string> All = [TodosRead, TodosWrite, TodosDelete];
}
#endif

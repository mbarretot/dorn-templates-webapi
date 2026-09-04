#if (UseCustomAuth)
using Microsoft.AspNetCore.Identity;

namespace CleanArchWebApi.Domain.Users;

public class AppUser : IdentityUser<Guid>
{
    /// <summary>Fine-grained permission strings (e.g. "todos:read") granted to this user; see
    /// CleanArchWebApi.Application.Common.Security.Permissions.</summary>
    public string[] Permissions { get; set; } = [];
}
#endif

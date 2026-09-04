#if (UseCustomAuth)
using System.Security.Cryptography;
using System.Text;

namespace CleanArchWebApi.Application.Common.Security;

/// <summary>
/// Hashes raw refresh token values before they are persisted. The server only ever stores
/// this hash (never the raw value) so a database leak cannot be used to impersonate users,
/// mirroring the spirit of <see cref="Microsoft.AspNetCore.Identity.IPasswordHasher{TUser}" />
/// already used for passwords.
/// </summary>
public static class RefreshTokenHasher
{
    public static string Hash(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes);
    }
}
#endif

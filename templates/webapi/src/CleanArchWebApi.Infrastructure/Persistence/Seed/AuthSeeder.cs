#if (UseCustomAuth)
using System.Security.Cryptography;
using CleanArchWebApi.Domain.Users;
using CleanArchWebApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CleanArchWebApi.Infrastructure.Persistence.Seed;

public static class AuthSeeder
{
    public static async Task<string> SeedAsync(
        IApplicationDbContext db,
        IPasswordHasher<AppUser> hasher,
        IOptions<AuthSeedOptions> options,
        CancellationToken ct
    )
    {
        var email = options.Value.DemoEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return string.Empty;
        }

        var password = GeneratePassword();

        db.Users.Add(
            new AppUser
            {
                Email = email,
                UserName = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                PasswordHash = hasher.HashPassword(null!, password),
            }
        );
        await db.SaveChangesAsync(ct);
        return password;
    }

    private static string GeneratePassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes);
    }
}
#endif
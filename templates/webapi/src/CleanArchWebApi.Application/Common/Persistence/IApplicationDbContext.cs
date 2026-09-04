#if (UseCustomAuth)
using CleanArchWebApi.Domain.Users;
#endif

namespace CleanArchWebApi.Application.Common.Persistence;

public interface IApplicationDbContext
{
#if (UseCustomAuth)
    DbSet<AppUser> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }
#endif

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

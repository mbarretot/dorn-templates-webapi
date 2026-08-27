#if (UseCustomAuth)
using CleanArchWebApi.Domain.Users;

namespace CleanArchWebApi.Application.Common.Security;

public interface ITokenService
{
    Task<TokenResult> CreateTokenAsync(AppUser user, CancellationToken cancellationToken);
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresAt);
#endif

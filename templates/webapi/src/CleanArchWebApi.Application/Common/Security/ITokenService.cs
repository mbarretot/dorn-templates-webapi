#if (UseCustomAuth)
using CleanArchWebApi.Domain.Users;

namespace CleanArchWebApi.Application.Common.Security;

public interface ITokenService
{
    Task<TokenResult> CreateTokenAsync(AppUser user, CancellationToken cancellationToken);

    RefreshTokenResult GenerateRefreshToken();
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresAt);

public sealed record RefreshTokenResult(string Token, DateTime ExpiresAt);
#endif

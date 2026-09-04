#if (UseCustomAuth)
namespace CleanArchWebApi.Application.Auth.Refresh;

public sealed record RefreshTokenResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);
#endif

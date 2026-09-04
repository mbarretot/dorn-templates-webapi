#if (UseCustomAuth)
namespace CleanArchWebApi.Application.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);
#endif

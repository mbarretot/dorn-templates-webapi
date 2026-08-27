#if (UseCustomAuth)
namespace CleanArchWebApi.Application.Auth.Login;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt);
#endif

#if (UseCustomAuth)
using Dorn.SharedKernel;

namespace CleanArchWebApi.Application.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<RefreshTokenResponse>>;
#endif

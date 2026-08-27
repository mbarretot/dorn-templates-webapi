#if (UseCustomAuth)
using Dorn.SharedKernel;

namespace CleanArchWebApi.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
#endif

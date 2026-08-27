#if (UseCustomAuth)
using CleanArchWebApi.Application.Common.Persistence;
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.Domain.Users;
using Dorn.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Auth.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        ITokenService tokenService
    )
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalizedEmail,
            ct
        );
        if (user is null)
        {
            return Result.Failure<LoginResponse>(InvalidCredentialsMessage);
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash!,
            request.Password
        );
        if (verification == PasswordVerificationResult.Failed)
        {
            return Result.Failure<LoginResponse>(InvalidCredentialsMessage);
        }

        var token = await _tokenService.CreateTokenAsync(user, ct);
        return Result.Success(new LoginResponse(token.AccessToken, token.ExpiresAt));
    }
}
#endif

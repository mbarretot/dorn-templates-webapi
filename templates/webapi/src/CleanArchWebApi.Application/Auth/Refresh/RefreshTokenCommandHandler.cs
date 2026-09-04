#if (UseCustomAuth)
using CleanArchWebApi.Application.Common.Persistence;
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.Domain.Users;
using Dorn.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Auth.Refresh;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private const string InvalidTokenMessage = "Invalid or expired refresh token.";

    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(IApplicationDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct
    )
    {
        var presentedHash = RefreshTokenHasher.Hash(request.RefreshToken);
        var presented = await _dbContext.RefreshTokens.FirstOrDefaultAsync(
            token => token.TokenHash == presentedHash,
            ct
        );

        if (presented is null)
        {
            return Result.Failure<RefreshTokenResponse>(InvalidTokenMessage);
        }

        if (presented.RevokedAt is not null)
        {
            // The presented token was already rotated away (or previously revoked), so this
            // is a replay of a token that should no longer exist client-side - most likely a
            // stolen token being used by an attacker. Treat it as a compromise signal and
            // revoke every other still-active token in this user's chain.
            await RevokeActiveChainAsync(presented.UserId, ct);
            return Result.Failure<RefreshTokenResponse>(InvalidTokenMessage);
        }

        if (presented.ExpiresAt <= DateTime.UtcNow)
        {
            return Result.Failure<RefreshTokenResponse>(InvalidTokenMessage);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == presented.UserId, ct);
        if (user is null)
        {
            return Result.Failure<RefreshTokenResponse>(InvalidTokenMessage);
        }

        var accessToken = await _tokenService.CreateTokenAsync(user, ct);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = RefreshTokenHasher.Hash(newRefreshToken.Token),
            ExpiresAt = newRefreshToken.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };
        _dbContext.RefreshTokens.Add(replacement);

        presented.RevokedAt = DateTime.UtcNow;
        presented.ReplacedByTokenId = replacement.Id;

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(
            new RefreshTokenResponse(
                accessToken.AccessToken,
                accessToken.ExpiresAt,
                newRefreshToken.Token,
                newRefreshToken.ExpiresAt
            )
        );
    }

    private async Task RevokeActiveChainAsync(Guid userId, CancellationToken ct)
    {
        var activeTokens = await _dbContext
            .RefreshTokens.Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(ct);

        if (activeTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
#endif

#if (UseCustomAuth)
using CleanArchWebApi.Application.Auth.Refresh;
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.Domain.Users;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Tests.Auth;

public sealed class RefreshTokenCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IPublisher _publisher;
    private readonly ApplicationDbContext _dbContext;

    public RefreshTokenCommandHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _publisher = Substitute.For<IPublisher>();
        _dbContext = new ApplicationDbContext(options, _publisher);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private async Task<AppUser> SeedUserAsync(string email)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            PasswordHash = "hashed:password",
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<RefreshToken> SeedRefreshTokenAsync(
        Guid userId,
        string rawToken,
        DateTime expiresAt,
        DateTime? revokedAt = null,
        Guid? replacedByTokenId = null
    )
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshTokenHasher.Hash(rawToken),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            RevokedAt = revokedAt,
            ReplacedByTokenId = replacedByTokenId,
        };
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();
        return token;
    }

    private static ITokenService CreateTokenService(
        string accessToken = "new-access-token",
        string refreshToken = "new-raw-refresh-token"
    )
    {
        var tokenService = Substitute.For<ITokenService>();
        tokenService
            .CreateTokenAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResult(accessToken, DateTime.UtcNow.AddMinutes(60)));
        tokenService
            .GenerateRefreshToken()
            .Returns(new RefreshTokenResult(refreshToken, DateTime.UtcNow.AddDays(7)));
        return tokenService;
    }

    [Fact]
    public async Task Handle_WithValidRefreshToken_RotatesAndReturnsNewTokenPair()
    {
        var user = await SeedUserAsync("demo@example.com");
        var oldToken = await SeedRefreshTokenAsync(
            user.Id,
            "raw-old-token",
            DateTime.UtcNow.AddDays(1)
        );
        var tokenService = CreateTokenService();
        var handler = new RefreshTokenCommandHandler(_dbContext, tokenService);

        var result = await handler.Handle(
            new RefreshTokenCommand("raw-old-token"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal("new-raw-refresh-token", result.Value.RefreshToken);

        await _dbContext.Entry(oldToken).ReloadAsync();
        Assert.NotNull(oldToken.RevokedAt);
        Assert.NotNull(oldToken.ReplacedByTokenId);

        var newToken = await _dbContext.RefreshTokens.SingleAsync(t =>
            t.Id == oldToken.ReplacedByTokenId
        );
        Assert.Equal(RefreshTokenHasher.Hash("new-raw-refresh-token"), newToken.TokenHash);
        Assert.Null(newToken.RevokedAt);
        Assert.Equal(user.Id, newToken.UserId);
    }

    [Fact]
    public async Task Handle_WithExpiredRefreshToken_ReturnsFailureAndDoesNotIssueNewTokens()
    {
        var user = await SeedUserAsync("demo@example.com");
        await SeedRefreshTokenAsync(user.Id, "raw-expired-token", DateTime.UtcNow.AddDays(-1));
        var tokenService = CreateTokenService();
        var handler = new RefreshTokenCommandHandler(_dbContext, tokenService);

        var result = await handler.Handle(
            new RefreshTokenCommand("raw-expired-token"),
            CancellationToken.None
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid or expired refresh token.", result.Error);
        await tokenService
            .DidNotReceive()
            .CreateTokenAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, await _dbContext.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsFailure()
    {
        var tokenService = CreateTokenService();
        var handler = new RefreshTokenCommandHandler(_dbContext, tokenService);

        var result = await handler.Handle(
            new RefreshTokenCommand("never-issued-token"),
            CancellationToken.None
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid or expired refresh token.", result.Error);
    }

    [Fact]
    public async Task Handle_WithReusedRevokedToken_ReturnsFailureAndRevokesTheWholeChain()
    {
        var user = await SeedUserAsync("demo@example.com");

        // Simulate a chain: tokenA was already rotated into tokenB, which is still active.
        var tokenB = await SeedRefreshTokenAsync(
            user.Id,
            "raw-token-b-active",
            DateTime.UtcNow.AddDays(1)
        );
        await SeedRefreshTokenAsync(
            user.Id,
            "raw-token-a-stolen",
            DateTime.UtcNow.AddDays(1),
            revokedAt: DateTime.UtcNow.AddMinutes(-5),
            replacedByTokenId: tokenB.Id
        );
        var tokenService = CreateTokenService();
        var handler = new RefreshTokenCommandHandler(_dbContext, tokenService);

        // An attacker replays the stolen (already-rotated) token A.
        var result = await handler.Handle(
            new RefreshTokenCommand("raw-token-a-stolen"),
            CancellationToken.None
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid or expired refresh token.", result.Error);
        await tokenService
            .DidNotReceive()
            .CreateTokenAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());

        // The compromise signal must revoke tokenB too, even though it was never presented.
        await _dbContext.Entry(tokenB).ReloadAsync();
        Assert.NotNull(tokenB.RevokedAt);
    }
}
#endif

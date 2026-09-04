#if (UseCustomAuth)
using CleanArchWebApi.Application.Auth.Login;
using CleanArchWebApi.Application.Common.Persistence;
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.Domain.Users;
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CleanArchWebApi.Application.Tests.Auth;

public sealed class LoginCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IPublisher _publisher;
    private readonly ApplicationDbContext _dbContext;

    public LoginCommandHandlerTests()
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

    private async Task<AppUser> SeedUserAsync(string email, string passwordHash)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            PasswordHash = passwordHash,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var email = TestCredentials.DemoEmail;
        await SeedUserAsync(email, "hashed:password");
        var passwordHasher = Substitute.For<IPasswordHasher<AppUser>>();
        passwordHasher
            .VerifyHashedPassword(Arg.Any<AppUser>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PasswordVerificationResult.Success);
        var tokenService = Substitute.For<ITokenService>();
        var expectedExpires = DateTime.UtcNow.AddMinutes(60);
        var expectedRefreshExpires = DateTime.UtcNow.AddDays(7);
        tokenService
            .CreateTokenAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResult("token-jws-value", expectedExpires));
        tokenService
            .GenerateRefreshToken()
            .Returns(new RefreshTokenResult("raw-refresh-token-value", expectedRefreshExpires));

        var handler = new LoginCommandHandler(_dbContext, passwordHasher, tokenService);
        var command = new LoginCommand(email, TestCredentials.DemoPassword);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("token-jws-value", result.Value.AccessToken);
        Assert.Equal(expectedExpires, result.Value.ExpiresAt);
        Assert.Equal("raw-refresh-token-value", result.Value.RefreshToken);
        Assert.Equal(expectedRefreshExpires, result.Value.RefreshTokenExpiresAt);
        await tokenService
            .Received(1)
            .CreateTokenAsync(Arg.Is<AppUser>(u => u.Email == email), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCredentials_PersistsOnlyTheHashedRefreshToken()
    {
        var email = TestCredentials.DemoEmail;
        var seeded = await SeedUserAsync(email, "hashed:password");
        var passwordHasher = Substitute.For<IPasswordHasher<AppUser>>();
        passwordHasher
            .VerifyHashedPassword(Arg.Any<AppUser>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PasswordVerificationResult.Success);
        var tokenService = Substitute.For<ITokenService>();
        tokenService
            .CreateTokenAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResult("token-jws-value", DateTime.UtcNow.AddMinutes(60)));
        var refreshExpires = DateTime.UtcNow.AddDays(7);
        tokenService
            .GenerateRefreshToken()
            .Returns(new RefreshTokenResult("raw-refresh-token-value", refreshExpires));

        var handler = new LoginCommandHandler(_dbContext, passwordHasher, tokenService);
        var command = new LoginCommand(email, TestCredentials.DemoPassword);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _dbContext.RefreshTokens.SingleAsync(t => t.UserId == seeded.Id);
        Assert.Equal(RefreshTokenHasher.Hash("raw-refresh-token-value"), stored.TokenHash);
        Assert.NotEqual("raw-refresh-token-value", stored.TokenHash);
        Assert.Equal(refreshExpires, stored.ExpiresAt);
        Assert.Null(stored.RevokedAt);
        Assert.Null(stored.ReplacedByTokenId);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsFailureWithGenericMessage()
    {
        var passwordHasher = Substitute.For<IPasswordHasher<AppUser>>();
        var tokenService = Substitute.For<ITokenService>();
        var handler = new LoginCommandHandler(_dbContext, passwordHasher, tokenService);
        var command = new LoginCommand("nobody@example.com", TestCredentials.DemoPassword);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid email or password.", result.Error);
        await tokenService
            .DidNotReceive()
            .CreateTokenAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsFailureWithSameGenericMessage()
    {
        var email = TestCredentials.DemoEmail;
        await SeedUserAsync(email, "hashed:password");
        var passwordHasher = Substitute.For<IPasswordHasher<AppUser>>();
        passwordHasher
            .VerifyHashedPassword(Arg.Any<AppUser>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PasswordVerificationResult.Failed);
        var tokenService = Substitute.For<ITokenService>();
        var handler = new LoginCommandHandler(_dbContext, passwordHasher, tokenService);
        var command = new LoginCommand(email, "WrongPassword!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid email or password.", result.Error);
        await tokenService
            .DidNotReceive()
            .CreateTokenAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCredentials_GeneratesJwsWithCorrectSubClaim()
    {
        var email = TestCredentials.DemoEmail;
        var seeded = await SeedUserAsync(email, "hashed:password");
        var passwordHasher = Substitute.For<IPasswordHasher<AppUser>>();
        passwordHasher
            .VerifyHashedPassword(Arg.Any<AppUser>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PasswordVerificationResult.Success);

        var tokenService = new RecordingTokenService();
        var handler = new LoginCommandHandler(_dbContext, passwordHasher, tokenService);
        var command = new LoginCommand(email, TestCredentials.DemoPassword);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.Id, tokenService.LastUserId);
    }

    private sealed class RecordingTokenService : ITokenService
    {
        public Guid? LastUserId { get; private set; }

        public Task<TokenResult> CreateTokenAsync(AppUser user, CancellationToken cancellationToken)
        {
            LastUserId = user.Id;
            return Task.FromResult(new TokenResult("token", DateTime.UtcNow.AddMinutes(60)));
        }

        public RefreshTokenResult GenerateRefreshToken() =>
            new("raw-refresh-token-value", DateTime.UtcNow.AddDays(7));
    }
}
#endif

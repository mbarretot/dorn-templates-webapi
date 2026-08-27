#if (UseCustomAuth)
using System.Text;
using System.Text.Json;
using CleanArchWebApi.Application.Common.Security;
using CleanArchWebApi.Domain.Users;
using CleanArchWebApi.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CleanArchWebApi.Application.Tests.Auth;

public sealed class JwtTokenServiceTests
{
    private const string TestSigningKey = "test-signing-key-with-at-least-32-bytes-for-hmacsha256!";
    private const string TestIssuer = "https://test.dorn.example";
    private const string TestAudience = "dorn-api-test";

    private static JwtTokenService CreateService()
    {
        var options = Options.Create(
            new JwtOptions
            {
                SigningKey = TestSigningKey,
                Issuer = TestIssuer,
                Audience = TestAudience,
                LifetimeMinutes = 60,
            }
        );
        return new JwtTokenService(options);
    }

    private static AppUser CreateUser()
    {
        return new AppUser
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserName = "demo@example.com",
            Email = "demo@example.com",
        };
    }

    private static JsonElement DecodePayload(string compactJws)
    {
        var segments = compactJws.Split('.');
        Assert.Equal(3, segments.Length);
        var payloadSegment = segments[1];
        var padded = payloadSegment.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }
        var bytes = Convert.FromBase64String(padded);
        var json = Encoding.UTF8.GetString(bytes);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public async Task CreateTokenAsync_ReturnsValidJwsCompactFormat()
    {
        var service = CreateService();
        var user = CreateUser();

        var result = await service.CreateTokenAsync(user, CancellationToken.None);

        Assert.NotNull(result.AccessToken);
        var segments = result.AccessToken.Split('.');
        Assert.Equal(3, segments.Length);
        Assert.All(segments, s => Assert.NotEmpty(s));
    }

    [Fact]
    public async Task CreateTokenAsync_PayloadContainsExpectedClaims()
    {
        var service = CreateService();
        var user = CreateUser();

        var result = await service.CreateTokenAsync(user, CancellationToken.None);

        var payload = DecodePayload(result.AccessToken);

        Assert.Equal(user.Id.ToString(), payload.GetProperty("sub").GetString());
        Assert.Equal(user.Email, payload.GetProperty("email").GetString());
        Assert.Equal(TestIssuer, payload.GetProperty("iss").GetString());

        var aud = payload.GetProperty("aud");
        var audValues =
            aud.ValueKind == JsonValueKind.Array
                ? aud.EnumerateArray().Select(e => e.GetString()).ToArray()
                : new[] { aud.GetString() };
        Assert.Contains(TestAudience, audValues);

        Assert.True(payload.TryGetProperty("jti", out var jti));
        Assert.False(string.IsNullOrEmpty(jti.GetString()));
    }

    [Fact]
    public async Task CreateTokenAsync_ExpiresAtIsApproximately60MinutesFromNow()
    {
        var service = CreateService();
        var user = CreateUser();
        var before = DateTime.UtcNow;

        var result = await service.CreateTokenAsync(user, CancellationToken.None);

        var after = DateTime.UtcNow;
        var payload = DecodePayload(result.AccessToken);
        var expUnix = payload.GetProperty("exp").GetInt64();
        var expUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

        Assert.InRange(
            expUtc,
            before.AddMinutes(60).AddSeconds(-1),
            after.AddMinutes(60).AddSeconds(1)
        );
        Assert.InRange(
            result.ExpiresAt,
            before.AddMinutes(60).AddSeconds(-1),
            after.AddMinutes(60).AddSeconds(1)
        );
    }
}

#endif

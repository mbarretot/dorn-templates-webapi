#if (UseCustomAuth)
namespace CleanArchWebApi.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 60;

    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
#endif

#if (UseCustomAuth)
namespace CleanArchWebApi.Infrastructure.Auth;

public sealed class AuthSeedOptions
{
    public const string SectionName = "AuthSeed";

    public string DemoEmail { get; set; } = string.Empty;
}
#endif

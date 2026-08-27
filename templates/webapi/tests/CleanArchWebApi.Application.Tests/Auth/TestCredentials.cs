#if (UseCustomAuth)
namespace CleanArchWebApi.Application.Tests.Auth;

internal static class TestCredentials
{
    public const string DemoEmail = "demo@example.com";

    public static readonly string DemoPassword = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)
    );
}
#endif

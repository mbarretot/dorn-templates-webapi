#if (UseDapper && !IncludeSample)
namespace CleanArchWebApi.Functional.Tests;

public sealed partial class ApiWebApplicationFactory
{
    partial void ConfigurePersistence(IWebHostBuilder builder) { }

    private partial Task InitializePersistenceAsync() => Task.CompletedTask;

    private partial Task DisposePersistenceAsync() => Task.CompletedTask;
}
#endif

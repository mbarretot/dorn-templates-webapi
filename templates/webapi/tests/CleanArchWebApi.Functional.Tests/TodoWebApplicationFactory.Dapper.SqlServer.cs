using CleanArchWebApi.Infrastructure.Repositories.Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace CleanArchWebApi.Functional.Tests;

public sealed partial class TodoWebApplicationFactory
{
    // Same image tag as docker-compose.SqlServer.yml, kept in sync deliberately.
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-latest"
    ).Build();

    partial void ConfigurePersistence(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DapperContext>();
            services.AddScoped(_ => new DapperContext(BuildTestConfiguration()));
        });
    }

    private IConfiguration BuildTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CleanArchWebApi"] = _container.GetConnectionString(),
                }
            )
            .Build();
    }

    private async partial Task InitializePersistenceAsync()
    {
        await _container.StartAsync();

        var context = new DapperContext(BuildTestConfiguration());
        await context.InitializeSchemaAsync();
    }

    private partial Task DisposePersistenceAsync() => _container.DisposeAsync().AsTask();
}

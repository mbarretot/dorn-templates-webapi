using CleanArchWebApi.Infrastructure.Repositories.Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanArchWebApi.Functional.Tests;

/// <summary>
/// DapperContext's connection type is baked in per DatabaseProvider at generation time, so
/// unlike EF Core this HTTP tier can't just swap to a local SQLite file for any provider — this
/// partial only ships for Orm=dapper + DatabaseProvider=sqlite (see template.json exclude rules).
/// SqlServer/Postgres + Dapper needs a Testcontainers-backed factory instead, tracked alongside
/// the rest of the Dapper integration-test matrix.
/// </summary>
public sealed partial class TodoWebApplicationFactory
{
    partial void ConfigurePersistence(IWebHostBuilder builder)
    {
        CreateSqliteSchema();

        builder.ConfigureServices(services =>
        {
            // DapperContext reads its connection string from IConfiguration at construction
            // time, and appsettings.json already defines "ConnectionStrings:Default" — later
            // IWebHostBuilder.UseSetting calls don't reliably outrank it, so replace the
            // service registration outright instead, mirroring the EF Core partial's approach.
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
                    ["ConnectionStrings:Default"] = $"Data Source={_databasePath}",
                }
            )
            .Build();
    }

    private void CreateSqliteSchema()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE TodoItems (Id TEXT PRIMARY KEY, Title TEXT NOT NULL, IsComplete INTEGER NOT NULL)";
        command.ExecuteNonQuery();
    }
}

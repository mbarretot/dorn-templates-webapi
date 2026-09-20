using System.Text.Json;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace Dorn.Templates.WebApi.Tests;

[Trait("Category", "Integration")]
[Collection(TemplatePackCollection.Name)]
public class ConnectionStringOptionTests
{
    // Quotes, backslashes, a JSON injection attempt, and '$', '#', ':' (Compose/YAML) on purpose.
    private const string HostileValue =
        "Server=db.example.com;Database=\"Orders\";Password=p\\w\"d\", \"Injected\": \"1\";Token=$HOME#x:y";

    [Fact]
    public async Task Generate_WithoutConnectionString_KeepsTheProviderDefaults()
    {
        await GeneratedProject.WithAsync(
            "DornConnDefaultApp",
            outputDirectory =>
            {
                using var appsettings = ReadWebApiAppsettings(
                    outputDirectory,
                    "DornConnDefaultApp"
                );
                var connectionStrings = appsettings.RootElement.GetProperty("ConnectionStrings");
                Assert.Equal(
                    "Data Source=app.db",
                    connectionStrings.GetProperty("Default").GetString()
                );
                Assert.Single(connectionStrings.EnumerateObject());
                return Task.CompletedTask;
            }
        );
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    public async Task Generate_WithoutConnectionStringOnAContainerProvider_DeclaresNoConnectionString(
        string databaseProvider
    )
    {
        await GeneratedProject.WithAsync(
            "DornConnDefaultContainerApp",
            outputDirectory =>
            {
                using var appsettings = ReadWebApiAppsettings(
                    outputDirectory,
                    "DornConnDefaultContainerApp"
                );
                Assert.False(appsettings.RootElement.TryGetProperty("ConnectionStrings", out _));
                return Task.CompletedTask;
            },
            "--DatabaseProvider",
            databaseProvider
        );
    }

    [Theory]
    [InlineData("efcore")]
    [InlineData("dapper")]
    public async Task Generate_SqliteWithConnectionString_WritesItVerbatimWithoutInjection(
        string orm
    )
    {
        await GeneratedProject.WithAsync(
            "DornConnSqliteApp",
            outputDirectory =>
            {
                using var appsettings = ReadWebApiAppsettings(outputDirectory, "DornConnSqliteApp");
                var connectionStrings = appsettings.RootElement.GetProperty("ConnectionStrings");
                Assert.Equal(HostileValue, connectionStrings.GetProperty("Default").GetString());
                Assert.Single(connectionStrings.EnumerateObject());
                Assert.False(appsettings.RootElement.TryGetProperty("Injected", out _));
                Assert.False(connectionStrings.TryGetProperty("Injected", out _));
                return Task.CompletedTask;
            },
            "--Orm",
            orm,
            "--ConnectionString",
            HostileValue
        );
    }

    [Theory]
    [InlineData("sqlserver", "aspire", "efcore")]
    [InlineData("sqlserver", "docker-compose", "efcore")]
    [InlineData("sqlserver", "none", "dapper")]
    [InlineData("postgres", "aspire", "dapper")]
    [InlineData("postgres", "docker-compose", "dapper")]
    [InlineData("postgres", "none", "efcore")]
    public async Task Generate_ContainerProviderWithConnectionString_WiresItWhereverTheConnectionIsResolved(
        string databaseProvider,
        string orchestrator,
        string orm
    )
    {
        const string name = "DornConnWiredApp";
        await GeneratedProject.WithAsync(
            name,
            outputDirectory =>
            {
                using (var appsettings = ReadWebApiAppsettings(outputDirectory, name))
                {
                    var connectionStrings = appsettings.RootElement.GetProperty(
                        "ConnectionStrings"
                    );
                    Assert.Equal(HostileValue, connectionStrings.GetProperty(name).GetString());
                    Assert.Single(connectionStrings.EnumerateObject());
                }

                if (orchestrator == "aspire")
                {
                    AssertAspireUsesTheConnectionString(outputDirectory, name);
                }
                else if (orchestrator == "docker-compose")
                {
                    AssertComposeUsesTheConnectionString(outputDirectory, name, databaseProvider);
                }

                return Task.CompletedTask;
            },
            "--DatabaseProvider",
            databaseProvider,
            "--Orchestrator",
            orchestrator,
            "--Orm",
            orm,
            "--ConnectionString",
            HostileValue
        );
    }

    [Fact]
    public async Task Generate_SqliteWithAspireAndConnectionString_LeavesTheAppHostWithoutADatabaseResource()
    {
        const string name = "DornConnSqliteAspireApp";
        await GeneratedProject.WithAsync(
            name,
            async outputDirectory =>
            {
                var appHost = await File.ReadAllTextAsync(
                    GeneratedProject.SourcePath(outputDirectory, $"{name}.AppHost", "AppHost.cs")
                );
                Assert.DoesNotContain("AddConnectionString", appHost, StringComparison.Ordinal);
                Assert.DoesNotContain("AddSqlServer", appHost, StringComparison.Ordinal);
                Assert.DoesNotContain("AddPostgres", appHost, StringComparison.Ordinal);

                using var appHostSettings = GeneratedProject.ReadJson(
                    outputDirectory,
                    "src",
                    $"{name}.AppHost",
                    "appsettings.json"
                );
                Assert.False(
                    appHostSettings.RootElement.TryGetProperty("ConnectionStrings", out _)
                );
            },
            "--ConnectionString",
            "Data Source=custom.db"
        );
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    public async Task Generate_ContainerProviderWithoutConnectionString_StillProvisionsTheBundledDatabase(
        string databaseProvider
    )
    {
        const string name = "DornConnBundledApp";
        await GeneratedProject.WithAsync(
            name,
            async outputDirectory =>
            {
                var appHost = await File.ReadAllTextAsync(
                    GeneratedProject.SourcePath(outputDirectory, $"{name}.AppHost", "AppHost.cs")
                );
                Assert.DoesNotContain("AddConnectionString", appHost, StringComparison.Ordinal);
                Assert.Contains(
                    databaseProvider == "sqlserver" ? "AddSqlServer" : "AddPostgres",
                    appHost,
                    StringComparison.Ordinal
                );
            },
            "--DatabaseProvider",
            databaseProvider
        );
    }

    [Theory]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    public async Task Generate_ComposeWithoutConnectionString_StillBundlesTheDatabaseService(
        string databaseProvider
    )
    {
        const string name = "DornConnComposeBundledApp";
        await GeneratedProject.WithAsync(
            name,
            outputDirectory =>
            {
                var services = LoadComposeServices(outputDirectory);
                Assert.True(
                    services.Children.ContainsKey(new YamlScalarNode(databaseProvider)),
                    $"Expected the bundled '{databaseProvider}' service without a ConnectionString."
                );
                return Task.CompletedTask;
            },
            "--DatabaseProvider",
            databaseProvider,
            "--Orchestrator",
            "docker-compose"
        );
    }

    [Theory]
    [InlineData("sqlite", "efcore", "aspire", "Data Source=custom.db;Cache=Shared")]
    [InlineData(
        "sqlserver",
        "efcore",
        "aspire",
        "Server=tcp:db.example.com,1433;Database=Orders;User Id=app;Password=x;TrustServerCertificate=true"
    )]
    [InlineData(
        "postgres",
        "dapper",
        "docker-compose",
        "Host=db.example.com;Port=5432;Database=orders;Username=app;Password=x"
    )]
    public async Task GenerateAndBuild_WithConnectionString_ProducesBuildableSolution(
        string databaseProvider,
        string orm,
        string orchestrator,
        string connectionString
    )
    {
        await GeneratedProject.WithBuiltSolutionAsync(
            "DornConnBuildApp",
            (outputDirectory, _) =>
            {
                using var appsettings = ReadWebApiAppsettings(outputDirectory, "DornConnBuildApp");
                var connectionStrings = appsettings.RootElement.GetProperty("ConnectionStrings");
                var key = databaseProvider == "sqlite" ? "Default" : "DornConnBuildApp";
                Assert.Equal(connectionString, connectionStrings.GetProperty(key).GetString());
                return Task.CompletedTask;
            },
            "--DatabaseProvider",
            databaseProvider,
            "--Orm",
            orm,
            "--Orchestrator",
            orchestrator,
            "--ConnectionString",
            connectionString
        );
    }

    private static JsonDocument ReadWebApiAppsettings(string outputDirectory, string name) =>
        GeneratedProject.ReadJson(outputDirectory, "src", $"{name}.WebApi", "appsettings.json");

    private static void AssertAspireUsesTheConnectionString(string outputDirectory, string name)
    {
        var appHostDirectory = $"{name}.AppHost";
        var appHost = File.ReadAllText(
            GeneratedProject.SourcePath(outputDirectory, appHostDirectory, "AppHost.cs")
        );
        Assert.Contains($"AddConnectionString(\"{name}\")", appHost, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSqlServer", appHost, StringComparison.Ordinal);
        Assert.DoesNotContain("AddPostgres", appHost, StringComparison.Ordinal);
        Assert.Contains("WithReference", appHost, StringComparison.Ordinal);

        using var appHostSettings = GeneratedProject.ReadJson(
            outputDirectory,
            "src",
            appHostDirectory,
            "appsettings.json"
        );
        var connectionStrings = appHostSettings.RootElement.GetProperty("ConnectionStrings");
        Assert.Equal(HostileValue, connectionStrings.GetProperty(name).GetString());
        Assert.Single(connectionStrings.EnumerateObject());
        Assert.False(appHostSettings.RootElement.TryGetProperty("Injected", out _));
    }

    private static void AssertComposeUsesTheConnectionString(
        string outputDirectory,
        string name,
        string databaseProvider
    )
    {
        var services = LoadComposeServices(outputDirectory);
        Assert.False(
            services.Children.ContainsKey(new YamlScalarNode(databaseProvider)),
            "A supplied ConnectionString means the database is external: no bundled database service."
        );

        var webapi = (YamlMappingNode)services.Children[new YamlScalarNode("webapi")];
        var environment = (YamlSequenceNode)webapi.Children[new YamlScalarNode("environment")];
        var entries = environment
            .Children.Select(node => ((YamlScalarNode)node).Value ?? string.Empty)
            .ToList();

        // Compose interpolates '$', so it must be escaped as '$$'.
        Assert.Contains($"ConnectionStrings__{name}={HostileValue.Replace("$", "$$")}", entries);
        Assert.Single(
            entries,
            entry => entry.StartsWith("ConnectionStrings__", StringComparison.Ordinal)
        );

        if (webapi.Children.TryGetValue(new YamlScalarNode("depends_on"), out var dependsOn))
        {
            Assert.DoesNotContain(databaseProvider, dependsOn.ToString(), StringComparison.Ordinal);
        }

        Assert.False(
            LoadComposeRoot(outputDirectory).Children.ContainsKey(new YamlScalarNode("volumes")),
            "The bundled database volume must go away together with the bundled database service."
        );
    }

    private static YamlMappingNode LoadComposeRoot(string outputDirectory)
    {
        var path = Path.Combine(outputDirectory, "docker-compose.yml");
        Assert.True(File.Exists(path), $"Expected compose file at '{path}'.");
        var yaml = new YamlStream();
        using var reader = new StringReader(File.ReadAllText(path));
        yaml.Load(reader);
        return (YamlMappingNode)yaml.Documents[0].RootNode;
    }

    private static YamlMappingNode LoadComposeServices(string outputDirectory) =>
        (YamlMappingNode)LoadComposeRoot(outputDirectory).Children[new YamlScalarNode("services")];
}

using Xunit;

namespace Dorn.Templates.WebApi.Tests;

/// <summary>APP-16: the <c>IncludeAgentRules</c> template parameter (AGENTS.md plus a CLAUDE.md that points to it).</summary>
[Trait("Category", "Integration")]
[Collection(TemplatePackCollection.Name)]
public class AgentRulesOptionTests
{
    private static readonly string[] AlwaysMentioned =
    [
        "AgentRulesApp.Domain",
        "AgentRulesApp.Application",
        "AgentRulesApp.Infrastructure",
        "AgentRulesApp.WebApi",
        "dotnet build",
        "dotnet format",
        "dotnet test",
        "Dorn.Messaging",
        "IRequestHandler",
    ];

    [Fact]
    public async Task Generate_ByDefault_DoesNotEmitAgentRules()
    {
        await GeneratedProject.WithAsync(
            "AgentRulesApp",
            outputDirectory =>
            {
                Assert.False(File.Exists(Path.Combine(outputDirectory, "AGENTS.md")));
                Assert.False(File.Exists(Path.Combine(outputDirectory, "CLAUDE.md")));
                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task Generate_WithAgentRules_ClaudeMdPointsToAgentsMd()
    {
        await GeneratedProject.WithAsync(
            "AgentRulesApp",
            async outputDirectory =>
            {
                var claude = await File.ReadAllTextAsync(
                    Path.Combine(outputDirectory, "CLAUDE.md")
                );
                Assert.Contains("@AGENTS.md", claude, StringComparison.Ordinal);
                Assert.True(
                    claude.Length < 600,
                    "CLAUDE.md is a pointer: the rules live in AGENTS.md only."
                );
            },
            "--IncludeAgentRules",
            "true"
        );
    }

    [Fact]
    public async Task Generate_WithAgentRulesAndDefaults_DescribesOnlyWhatWasGenerated()
    {
        await GeneratedProject.WithAsync(
            "AgentRulesApp",
            async outputDirectory =>
            {
                var agents = await ReadAgentsAsync(outputDirectory);

                AssertMentions(
                    agents,
                    [
                        .. AlwaysMentioned,
                        "EF Core",
                        "SQLite",
                        "Aspire",
                        "AgentRulesApp.AppHost",
                        "dotnet ef migrations add",
                        "AgentRulesApp.Application.Tests",
                        "AgentRulesApp.Integration.Tests",
                        "AgentRulesApp.Architecture.Tests",
                        "AgentRulesApp.Functional.Tests",
                        "dotnet dorn test",
                        "Todos",
                    ]
                );
                AssertOmits(
                    agents,
                    "Dapper",
                    "SQL Server",
                    "PostgreSQL",
                    "Docker",
                    "Testcontainers",
                    "Jwt",
                    "JWT",
                    "Entra",
                    "Azure",
                    "docker compose",
                    "InitializeSchemaAsync",
                    "Permissions"
                );
                AssertRendered(agents);
            },
            "--IncludeAgentRules",
            "true"
        );
    }

    [Fact]
    public async Task Generate_WithAgentRulesDapperPostgresComposeAndAzureAd_DescribesOnlyWhatWasGenerated()
    {
        await GeneratedProject.WithAsync(
            "AgentRulesApp",
            async outputDirectory =>
            {
                var agents = await ReadAgentsAsync(outputDirectory);

                AssertMentions(
                    agents,
                    [
                        .. AlwaysMentioned,
                        "Dapper",
                        "InitializeSchemaAsync",
                        "NotSupportedException",
                        "PostgreSQL",
                        "Testcontainers",
                        "docker compose up",
                        "Entra",
                        "Permissions",
                        "AgentRulesApp.Integration.Tests",
                    ]
                );
                AssertOmits(
                    agents,
                    "EF Core",
                    "dotnet ef",
                    "migrations",
                    "SQLite",
                    "SQL Server",
                    "Aspire",
                    "AppHost",
                    "Jwt",
                    "JWT",
                    "AuthSeed",
                    "demo user"
                );
                AssertRendered(agents);
            },
            "--IncludeAgentRules",
            "true",
            "--Orm",
            "dapper",
            "--DatabaseProvider",
            "postgres",
            "--Orchestrator",
            "docker-compose",
            "--Auth",
            "azure-ad"
        );
    }

    [Fact]
    public async Task Generate_WithAgentRulesEfCoreSqlServerNoOrchestratorAndCustomAuth_DescribesOnlyWhatWasGenerated()
    {
        await GeneratedProject.WithAsync(
            "AgentRulesApp",
            async outputDirectory =>
            {
                var agents = await ReadAgentsAsync(outputDirectory);

                AssertMentions(
                    agents,
                    [
                        .. AlwaysMentioned,
                        "EF Core",
                        "dotnet ef migrations add",
                        "SQL Server",
                        "Testcontainers",
                        "dotnet run --project src/AgentRulesApp.WebApi",
                        "Jwt:SigningKey",
                        "AuthSeed",
                        "Permissions",
                    ]
                );
                AssertOmits(
                    agents,
                    "Dapper",
                    "PostgreSQL",
                    "SQLite",
                    "Aspire",
                    "AppHost",
                    "docker compose",
                    "Entra",
                    "Azure"
                );
                AssertRendered(agents);
            },
            "--IncludeAgentRules",
            "true",
            "--DatabaseProvider",
            "sqlserver",
            "--Orchestrator",
            "none",
            "--Auth",
            "custom"
        );
    }

    [Fact]
    public async Task Generate_WithAgentRulesAndWithoutTests_SaysSoAndOmitsTestCommands()
    {
        await GeneratedProject.WithAsync(
            "AgentRulesApp",
            async outputDirectory =>
            {
                var agents = await ReadAgentsAsync(outputDirectory);

                AssertMentions(
                    agents,
                    [.. AlwaysMentioned.Except(["dotnet test"]), "IncludeTests=false"]
                );
                AssertOmits(
                    agents,
                    "dotnet test",
                    "dotnet dorn test",
                    "Architecture.Tests",
                    "Functional.Tests",
                    "Integration.Tests",
                    "Application.Tests",
                    "NSubstitute",
                    "xUnit"
                );
                AssertRendered(agents);
            },
            "--IncludeAgentRules",
            "true",
            "--IncludeTests",
            "false"
        );
    }

    private static async Task<string> ReadAgentsAsync(string outputDirectory)
    {
        var path = Path.Combine(outputDirectory, "AGENTS.md");
        Assert.True(File.Exists(path), $"Expected '{path}' with IncludeAgentRules=true.");
        return await File.ReadAllTextAsync(path);
    }

    private static void AssertMentions(string text, IEnumerable<string> expected)
    {
        foreach (var fragment in expected)
        {
            Assert.True(
                text.Contains(fragment, StringComparison.Ordinal),
                $"AGENTS.md should mention '{fragment}'."
            );
        }
    }

    private static void AssertOmits(string text, params string[] unexpected)
    {
        foreach (var fragment in unexpected)
        {
            Assert.False(
                text.Contains(fragment, StringComparison.OrdinalIgnoreCase),
                $"AGENTS.md must not mention '{fragment}': it was not generated."
            );
        }
    }

    private static void AssertRendered(string text)
    {
        Assert.DoesNotContain("<!--#", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CleanArchWebApi", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__DORN_", text, StringComparison.Ordinal);
    }
}

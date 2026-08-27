using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace Dorn.Templates.WebApi.Tests;

[Trait("Category", "Integration")]
[Collection(TemplatePackCollection.Name)]
public class CiWorkflowTests
{
    /// <summary>Logs route via otlphttp, not the collector's deprecated dedicated loki exporter.</summary>
    [Fact]
    public async Task GivenDockerComposeOrchestrator_EmitsValidObservabilityConfigFiles()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornOtelComposeConfigApp",
            outputDirectory =>
            {
                var pipelines = GetMapping(
                    GetMapping(
                        LoadYamlMappingRoot(
                            Path.Combine(outputDirectory, "otel-collector-config.yaml")
                        ),
                        "service"
                    ),
                    "pipelines"
                );

                Assert.Contains("otlp/tempo", GetExporterNames(pipelines, "traces"));
                Assert.Contains("otlphttp/loki", GetExporterNames(pipelines, "logs"));
                Assert.Contains("prometheusremotewrite", GetExporterNames(pipelines, "metrics"));

                LoadYamlMappingRoot(Path.Combine(outputDirectory, "tempo.yaml"));
                LoadYamlMappingRoot(
                    Path.Combine(
                        outputDirectory,
                        "grafana",
                        "provisioning",
                        "datasources",
                        "datasources.yaml"
                    )
                );

                return Task.CompletedTask;
            },
            orchestrator: "docker-compose"
        );
    }

    /// <summary>Also asserts the port-publishing asymmetry and the no-:latest tag guard.</summary>
    [Fact]
    public async Task GivenDockerComposeOrchestrator_ComposeFileDeclaresObservabilityServices()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornOtelComposeServicesApp",
            async outputDirectory =>
            {
                var composePath = Path.Combine(outputDirectory, "docker-compose.yml");
                var composeContent = await File.ReadAllTextAsync(composePath);
                Assert.DoesNotContain(":latest", composeContent, StringComparison.Ordinal);

                var services = GetMapping(LoadYamlMappingRoot(composePath), "services");

                foreach (
                    var serviceName in new[]
                    {
                        "otel-collector",
                        "grafana",
                        "loki",
                        "prometheus",
                        "tempo",
                    }
                )
                {
                    Assert.True(
                        TryGetChild(services, serviceName) is not null,
                        $"docker-compose.yml must declare a '{serviceName}' service"
                    );
                }

                foreach (var serviceName in new[] { "loki", "prometheus", "tempo" })
                {
                    Assert.Null(TryGetChild(GetMapping(services, serviceName), "ports"));
                }

                foreach (var serviceName in new[] { "grafana", "otel-collector" })
                {
                    Assert.NotNull(TryGetChild(GetMapping(services, serviceName), "ports"));
                }

                var webapiEnvironment = GetSequence(GetMapping(services, "webapi"), "environment")
                    .Children.Select(node => ((YamlScalarNode)node).Value)
                    .ToList();
                Assert.Contains(
                    webapiEnvironment,
                    value =>
                        value is not null
                        && value.StartsWith(
                            "OTEL_EXPORTER_OTLP_ENDPOINT=",
                            StringComparison.Ordinal
                        )
                );
            },
            orchestrator: "docker-compose"
        );
    }

    /// <summary>Covers the map-form depends_on the base sqlite compose file never needed.</summary>
    [Theory]
    [InlineData("sqlserver", "sqlserver")]
    [InlineData("postgres", "postgres")]
    public async Task GivenDockerComposeOrchestrator_DbVariantComposeFileDeclaresObservabilityServices(
        string databaseProvider,
        string dbServiceName
    )
    {
        await WithGeneratedWebApiProjectAsync(
            $"DornOtelCompose{databaseProvider}App",
            async outputDirectory =>
            {
                var composePath = Path.Combine(outputDirectory, "docker-compose.yml");
                var composeContent = await File.ReadAllTextAsync(composePath);
                Assert.DoesNotContain(":latest", composeContent, StringComparison.Ordinal);

                var services = GetMapping(LoadYamlMappingRoot(composePath), "services");

                foreach (
                    var serviceName in new[]
                    {
                        "otel-collector",
                        "grafana",
                        "loki",
                        "prometheus",
                        "tempo",
                    }
                )
                {
                    Assert.True(
                        TryGetChild(services, serviceName) is not null,
                        $"docker-compose.yml (from {databaseProvider} variant) must declare a '{serviceName}' service"
                    );
                }

                foreach (var serviceName in new[] { "loki", "prometheus", "tempo" })
                {
                    Assert.Null(TryGetChild(GetMapping(services, serviceName), "ports"));
                }

                foreach (var serviceName in new[] { "grafana", "otel-collector" })
                {
                    Assert.NotNull(TryGetChild(GetMapping(services, serviceName), "ports"));
                }

                var webapi = GetMapping(services, "webapi");
                var webapiEnvironment = GetSequence(webapi, "environment")
                    .Children.Select(node => ((YamlScalarNode)node).Value)
                    .ToList();
                Assert.Contains(
                    webapiEnvironment,
                    value =>
                        value is not null
                        && value.StartsWith(
                            "OTEL_EXPORTER_OTLP_ENDPOINT=",
                            StringComparison.Ordinal
                        )
                );

                var webapiDependsOn = GetMapping(webapi, "depends_on");
                var dbDependsOn = GetMapping(webapiDependsOn, dbServiceName);
                Assert.Equal("service_healthy", GetScalar(dbDependsOn, "condition"));

                var otelDependsOn = GetMapping(webapiDependsOn, "otel-collector");
                Assert.Equal("service_started", GetScalar(otelDependsOn, "condition"));
            },
            orchestrator: "docker-compose",
            databaseProvider: databaseProvider
        );
    }

    [Fact]
    public async Task GlobalJson_IsEmittedAtRepositoryRootWithPinnedSdkVersion()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiGlobalJsonApp",
            outputDirectory =>
            {
                var nestedGlobalJson = Directory
                    .EnumerateFiles(outputDirectory, "global.json", SearchOption.AllDirectories)
                    .ToList();
                Assert.Single(nestedGlobalJson);

                using var document = ReadJsonFile(outputDirectory, "global.json");
                var sdkVersion = document
                    .RootElement.GetProperty("sdk")
                    .GetProperty("version")
                    .GetString();

                using var repoGlobalJson = JsonDocument.Parse(
                    File.ReadAllText(Path.Combine(TemplatePackHarness.RepoRoot, "global.json"))
                );
                var expectedSdkVersion = repoGlobalJson
                    .RootElement.GetProperty("sdk")
                    .GetProperty("version")
                    .GetString();

                Assert.Equal(expectedSdkVersion, sdkVersion);
                return Task.CompletedTask;
            }
        );
    }

    /// <summary>Verifies every generation emits a parseable CI workflow with the expected top-level keys.</summary>
    [Theory]
    [InlineData("aspire")]
    [InlineData("none")]
    public async Task CiWorkflow_IsEmittedAndParses_ForAllSymbols(string orchestrator)
    {
        await WithGeneratedWebApiProjectAsync(
            $"DornCiParse{orchestrator.Replace("-", "", StringComparison.Ordinal)}App",
            outputDirectory =>
            {
                var root = LoadCiWorkflowRoot(outputDirectory);
                Assert.True(root.Children.ContainsKey(new YamlScalarNode("on")));
                Assert.True(root.Children.ContainsKey(new YamlScalarNode("jobs")));
                Assert.Equal("CI", GetScalar(root, "name"));
                return Task.CompletedTask;
            },
            orchestrator: orchestrator,
            databaseProvider: "sqlite"
        );
    }

    /// <summary>
    /// Requirement "Matrix Shape": exactly two axes — os (2 values) and orchestrator (3
    /// values) — for a 6-cell matrix. DatabaseProvider MUST NOT be an axis.
    /// </summary>
    [Fact]
    public async Task CiWorkflow_HasSixCellMatrix()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiMatrixApp",
            outputDirectory =>
            {
                var root = LoadCiWorkflowRoot(outputDirectory);
                var jobs = GetMapping(root, "jobs");
                var buildAndTest = GetMapping(jobs, "build-and-test");
                var strategy = GetMapping(buildAndTest, "strategy");
                var matrix = GetMapping(strategy, "matrix");

                var os = GetSequence(matrix, "os");
                var orchestrator = GetSequence(matrix, "orchestrator");

                Assert.Equal(2, os.Children.Count);
                Assert.Equal(3, orchestrator.Children.Count);
                Assert.False(matrix.Children.ContainsKey(new YamlScalarNode("database")));

                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Requirement "Setup and Cache Steps": checkout@v7 and setup-dotnet@v6 (reading the
    /// repository-root global.json) run before a NuGet cache keyed on Directory.Packages.props.
    /// </summary>
    [Fact]
    public async Task CiWorkflow_PinsSetupAndCacheActions()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiSetupCacheApp",
            outputDirectory =>
            {
                var rawText = ReadCiWorkflowRawText(outputDirectory);
                Assert.Contains(
                    "global-json-file: ./global.json",
                    rawText,
                    StringComparison.Ordinal
                );
                Assert.Contains("Directory.Packages.props", rawText, StringComparison.Ordinal);

                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var checkoutIndex = steps.FindIndex(s =>
                    s.Uses?.StartsWith("actions/checkout@v7", StringComparison.Ordinal) == true
                );
                var setupDotnetIndex = steps.FindIndex(s =>
                    s.Uses?.StartsWith("actions/setup-dotnet@v6", StringComparison.Ordinal) == true
                );
                var cacheIndex = steps.FindIndex(s =>
                    s.Uses?.StartsWith("actions/cache@v6", StringComparison.Ordinal) == true
                );

                Assert.True(checkoutIndex >= 0);
                Assert.True(setupDotnetIndex > checkoutIndex);
                Assert.True(cacheIndex > setupDotnetIndex);

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_RestoresBeforeBuildWithRaceFlags()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiRestoreBuildApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var restoreIndex = steps.FindIndex(s =>
                    s.Run.Contains("dotnet restore", StringComparison.Ordinal)
                );
                var buildIndex = steps.FindIndex(s =>
                    s.Run.Contains("dotnet build", StringComparison.Ordinal)
                );

                Assert.True(restoreIndex >= 0);
                Assert.True(buildIndex >= 0);
                Assert.True(restoreIndex < buildIndex);

                var restoreCommand = steps[restoreIndex].Run;
                Assert.Contains("-maxCpuCount:1", restoreCommand, StringComparison.Ordinal);
                Assert.Contains("-nodeReuse:false", restoreCommand, StringComparison.Ordinal);

                var buildCommand = steps[buildIndex].Run;
                Assert.Contains("-c Release", buildCommand, StringComparison.Ordinal);
                Assert.Contains("--no-restore", buildCommand, StringComparison.Ordinal);

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_DefaultTestRunsAllTiersOnce()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiDefaultTestApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var context = new Dictionary<string, string> { ["inputs.exclude_tiers"] = "" };

                var activeTestCommands = steps
                    .Where(s => s.Run.Contains("dotnet test", StringComparison.Ordinal))
                    .Where(s => s.If is null || EvaluateGithubActionsExpression(s.If, context))
                    .Select(s => s.Run)
                    .ToList();

                var command = Assert.Single(activeTestCommands);
                Assert.Contains("--no-build", command, StringComparison.Ordinal);
                Assert.Contains("-c Release", command, StringComparison.Ordinal);
                Assert.Contains(
                    "--collect:\"XPlat Code Coverage\"",
                    command,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain("--filter", command, StringComparison.Ordinal);

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_ExclusionRunsRemainingTiers()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiExclusionApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var context = new Dictionary<string, string>
                {
                    ["inputs.exclude_tiers"] = "Integration",
                };

                var activeTestSteps = steps
                    .Where(s => s.Run.Contains("dotnet test", StringComparison.Ordinal))
                    .Where(s => s.If is null || EvaluateGithubActionsExpression(s.If, context))
                    .ToList();

                Assert.Equal(3, activeTestSteps.Count);
                Assert.All(activeTestSteps, s => Assert.NotNull(s.If));
                Assert.DoesNotContain(
                    activeTestSteps,
                    s => s.Run.Contains("Integration.Tests", StringComparison.Ordinal)
                );

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_SqliteStartsNoService()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiSqliteServiceApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var context = new Dictionary<string, string>
                {
                    ["needs.configuration.outputs.db"] = "sqlite",
                    ["runner.os"] = "Linux",
                };

                var sqlServerSteps = steps
                    .Where(s =>
                        s.Run.Contains("mcr.microsoft.com/azure-sql-edge", StringComparison.Ordinal)
                    )
                    .ToList();
                Assert.NotEmpty(sqlServerSteps);
                Assert.All(
                    sqlServerSteps,
                    s =>
                        Assert.False(
                            s.If is not null && EvaluateGithubActionsExpression(s.If, context),
                            $"Step '{s.Name}' would execute for a sqlite marker."
                        )
                );

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_LinuxSqlServerUsesHealthyEdge()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiLinuxSqlServerApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var context = new Dictionary<string, string>
                {
                    ["needs.configuration.outputs.db"] = "sqlserver",
                    ["runner.os"] = "Linux",
                };

                var startIndex = steps.FindIndex(s =>
                    s.Run.Contains("mcr.microsoft.com/azure-sql-edge", StringComparison.Ordinal)
                );
                Assert.True(startIndex >= 0);
                Assert.NotNull(steps[startIndex].If);
                Assert.True(EvaluateGithubActionsExpression(steps[startIndex].If!, context));

                var healthCheckIndex = steps.FindIndex(s =>
                    s.Run.Contains("sqlcmd", StringComparison.Ordinal)
                    && s.Run.Contains("-Q \"select 1\"", StringComparison.Ordinal)
                );
                Assert.True(healthCheckIndex >= 0);

                var testIndex = steps.FindIndex(s =>
                    s.Run.Contains("dotnet test", StringComparison.Ordinal)
                );
                Assert.True(testIndex >= 0);
                Assert.True(healthCheckIndex < testIndex);

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_LinuxPostgresUsesHealthyContainer()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiLinuxPostgresApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var context = new Dictionary<string, string>
                {
                    ["needs.configuration.outputs.db"] = "postgres",
                    ["runner.os"] = "Linux",
                };

                var startIndex = steps.FindIndex(s =>
                    s.Run.Contains("postgres:17", StringComparison.Ordinal)
                    && s.Run.Contains("docker run", StringComparison.Ordinal)
                );
                Assert.True(startIndex >= 0);
                Assert.NotNull(steps[startIndex].If);
                Assert.True(EvaluateGithubActionsExpression(steps[startIndex].If!, context));

                var healthCheckIndex = steps.FindIndex(s =>
                    s.Run.Contains("pg_isready", StringComparison.Ordinal)
                );
                Assert.True(healthCheckIndex >= 0);

                var testIndex = steps.FindIndex(s =>
                    s.Run.Contains("dotnet test", StringComparison.Ordinal)
                );
                Assert.True(testIndex >= 0);
                Assert.True(healthCheckIndex < testIndex);

                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_SqliteStartsNoPostgresService()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiSqlitePostgresServiceApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var context = new Dictionary<string, string>
                {
                    ["needs.configuration.outputs.db"] = "sqlite",
                    ["runner.os"] = "Linux",
                };

                var postgresSteps = steps
                    .Where(s => s.Run.Contains("postgres:17", StringComparison.Ordinal))
                    .ToList();
                Assert.NotEmpty(postgresSteps);
                Assert.All(
                    postgresSteps,
                    s =>
                        Assert.False(
                            s.If is not null && EvaluateGithubActionsExpression(s.If, context),
                            $"Step '{s.Name}' would execute for a sqlite marker."
                        )
                );

                return Task.CompletedTask;
            }
        );
    }

    /// <summary>Uses Testcontainers as a .NET library, not a CLI.</summary>
    [Fact]
    public async Task CiWorkflow_WindowsPostgresIsBestEffort()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiWindowsPostgresApp",
            outputDirectory =>
            {
                var rawText = ReadCiWorkflowRawText(outputDirectory);
                var branchIndex = rawText.IndexOf(
                    "Windows + PostgreSQL caveat",
                    StringComparison.Ordinal
                );
                Assert.True(branchIndex >= 0);

                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                Assert.All(
                    steps,
                    s =>
                        Assert.DoesNotContain(
                            "testcontainers",
                            s.Run,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

                return Task.CompletedTask;
            }
        );
    }

    /// <summary>Uses Testcontainers as a .NET library, not a CLI.</summary>
    [Fact]
    public async Task CiWorkflow_WindowsSqlServerIsBestEffort()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiWindowsSqlServerApp",
            outputDirectory =>
            {
                var rawText = ReadCiWorkflowRawText(outputDirectory);
                var commentIndex = rawText.IndexOf("# best-effort:", StringComparison.Ordinal);
                var branchIndex = rawText.IndexOf(
                    "Windows + SQL Server caveat",
                    StringComparison.Ordinal
                );
                Assert.True(commentIndex >= 0);
                Assert.True(branchIndex > commentIndex);

                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                Assert.All(
                    steps,
                    s =>
                        Assert.DoesNotContain(
                            "testcontainers",
                            s.Run,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Requirement "ORM Compatibility": no hardcoded `dotnet ef` calls anywhere in the
    /// workflow — migrations-on-startup is a runtime concern inside the app, not CI.
    /// </summary>
    [Fact]
    public async Task CiWorkflow_DoesNotInvokeEfCli()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiNoEfCliApp",
            outputDirectory =>
            {
                Assert.DoesNotContain(
                    "dotnet ef",
                    ReadCiWorkflowRawText(outputDirectory),
                    StringComparison.Ordinal
                );
                return Task.CompletedTask;
            }
        );
    }

    [Fact]
    public async Task CiWorkflow_AggregatesCoverageOnUbuntuOnly()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiCoverageApp",
            outputDirectory =>
            {
                var steps = GetSteps(LoadCiWorkflowRoot(outputDirectory), "build-and-test");
                var coverageStep = steps.Single(s =>
                    s.Run.Contains("reportgenerator", StringComparison.OrdinalIgnoreCase)
                );

                Assert.Contains(
                    "**/coverage.cobertura.xml",
                    coverageStep.Run,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    "-assemblyfilters:+:-*.Tests",
                    coverageStep.Run,
                    StringComparison.Ordinal
                );
                Assert.Equal("matrix.os == 'ubuntu-latest'", coverageStep.If);

                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Requirement "Marker File Emission" (SQLite): `--database sqlite` emits
    /// `.github/config/db-provider.txt` equal to `sqlite`.
    /// </summary>
    [Fact]
    public async Task SqliteMarker_IsEmitted()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiSqliteMarkerApp",
            outputDirectory =>
            {
                var markerPath = Path.Combine(
                    outputDirectory,
                    ".github",
                    "config",
                    "db-provider.txt"
                );
                Assert.True(File.Exists(markerPath), $"Expected marker file at '{markerPath}'.");
                Assert.Equal("sqlite", File.ReadAllText(markerPath).Trim());
                return Task.CompletedTask;
            },
            databaseProvider: "sqlite"
        );
    }

    /// <summary>
    /// Requirement "Marker File Emission" (SQL Server): `--database sqlserver` emits
    /// `.github/config/db-provider.txt` equal to `sqlserver`.
    /// </summary>
    [Fact]
    public async Task SqlServerMarker_IsEmitted()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiSqlServerMarkerApp",
            outputDirectory =>
            {
                var markerPath = Path.Combine(
                    outputDirectory,
                    ".github",
                    "config",
                    "db-provider.txt"
                );
                Assert.True(File.Exists(markerPath), $"Expected marker file at '{markerPath}'.");
                Assert.Equal("sqlserver", File.ReadAllText(markerPath).Trim());
                return Task.CompletedTask;
            },
            databaseProvider: "sqlserver"
        );
    }

    /// <summary>
    /// Requirement "Marker File Emission" (PostgreSQL): `--database postgres` emits
    /// `.github/config/db-provider.txt` equal to `postgres`.
    /// </summary>
    [Fact]
    public async Task PostgresMarker_IsEmitted()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiPostgresMarkerApp",
            outputDirectory =>
            {
                var markerPath = Path.Combine(
                    outputDirectory,
                    ".github",
                    "config",
                    "db-provider.txt"
                );
                Assert.True(File.Exists(markerPath), $"Expected marker file at '{markerPath}'.");
                Assert.Equal("postgres", File.ReadAllText(markerPath).Trim());
                return Task.CompletedTask;
            },
            databaseProvider: "postgres"
        );
    }

    /// <summary>No out-of-scope packaging, Dependabot, or README badge steps.</summary>
    [Fact]
    public async Task CiWorkflow_ContainsNoOutOfScopeSteps()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiOutOfScopeApp",
            outputDirectory =>
            {
                var rawText = ReadCiWorkflowRawText(outputDirectory);
                Assert.DoesNotContain("dotnet pack", rawText, StringComparison.Ordinal);
                Assert.DoesNotContain("dotnet nuget push", rawText, StringComparison.Ordinal);
                Assert.DoesNotContain("dependabot", rawText, StringComparison.Ordinal);
                Assert.DoesNotContain("badge", rawText, StringComparison.Ordinal);
                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Re-runs the structural workflow contract for the representative efcore/aspire/sqlite cell.
    /// </summary>
    [Fact]
    public async Task CiWorkflow_StructuralContract_HoldsAcrossMatrix()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiAggregateApp",
            outputDirectory =>
            {
                var root = LoadCiWorkflowRoot(outputDirectory);
                Assert.True(root.Children.ContainsKey(new YamlScalarNode("on")));
                Assert.True(root.Children.ContainsKey(new YamlScalarNode("jobs")));

                var on = GetMapping(root, "on");
                Assert.True(on.Children.ContainsKey(new YamlScalarNode("push")));
                Assert.True(on.Children.ContainsKey(new YamlScalarNode("pull_request")));
                Assert.True(on.Children.ContainsKey(new YamlScalarNode("workflow_dispatch")));

                var jobs = GetMapping(root, "jobs");
                var buildAndTest = GetMapping(jobs, "build-and-test");
                var matrix = GetMapping(GetMapping(buildAndTest, "strategy"), "matrix");
                Assert.Equal(2, GetSequence(matrix, "os").Children.Count);
                Assert.Equal(3, GetSequence(matrix, "orchestrator").Children.Count);

                var rawText = ReadCiWorkflowRawText(outputDirectory);
                Assert.DoesNotContain("dotnet ef", rawText, StringComparison.Ordinal);
                Assert.DoesNotContain("dotnet pack", rawText, StringComparison.Ordinal);

                using var globalJson = ReadJsonFile(outputDirectory, "global.json");
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString()
                    )
                );

                return Task.CompletedTask;
            },
            orm: "efcore",
            orchestrator: "aspire",
            databaseProvider: "sqlite"
        );
    }

    /// <summary>
    /// Builds and tests the representative efcore/none/sqlite cell while validating its workflow YAML.
    /// </summary>
    [Fact]
    public async Task GeneratedCheapestCell_BuildsTestsAndHasValidWorkflow()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiSmokeApp",
            async outputDirectory =>
            {
                LoadCiWorkflowRoot(outputDirectory);

                var slnFiles = Directory.GetFiles(
                    outputDirectory,
                    "*.slnx",
                    SearchOption.TopDirectoryOnly
                );
                Assert.Single(slnFiles);

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnFiles[0]);
                WebApiTemplateGenerationTests.AssertBuildSucceeded(buildResult);

                var testResult = await TemplatePackHarness.RunProcessAsync(
                    Path.GetDirectoryName(slnFiles[0])!,
                    null,
                    "test",
                    slnFiles[0],
                    "-c",
                    "Release",
                    "--no-build"
                );
                Assert.True(
                    testResult.ExitCode == 0,
                    $"dotnet test exited with {testResult.ExitCode}."
                        + $"{Environment.NewLine}STDOUT:{Environment.NewLine}{testResult.StdOut}"
                        + $"{Environment.NewLine}STDERR:{Environment.NewLine}{testResult.StdErr}"
                );
            },
            orm: "efcore",
            orchestrator: "none",
            databaseProvider: "sqlite"
        );
    }

    /// <summary>
    /// Requirement "Triggers": the emitted workflow must trigger on push, pull_request, and
    /// workflow_dispatch only — no cron schedule, no path filters.
    /// </summary>
    [Fact]
    public async Task CiWorkflow_DeclaresExactlySupportedTriggers()
    {
        await WithGeneratedWebApiProjectAsync(
            "DornCiTriggersApp",
            outputDirectory =>
            {
                var root = LoadCiWorkflowRoot(outputDirectory);
                var on = GetMapping(root, "on");

                Assert.True(on.Children.ContainsKey(new YamlScalarNode("push")));
                Assert.True(on.Children.ContainsKey(new YamlScalarNode("pull_request")));
                Assert.True(on.Children.ContainsKey(new YamlScalarNode("workflow_dispatch")));
                Assert.False(on.Children.ContainsKey(new YamlScalarNode("schedule")));

                var rawText = ReadCiWorkflowRawText(outputDirectory);
                Assert.DoesNotContain("paths:", rawText, StringComparison.Ordinal);
                Assert.DoesNotContain("paths-ignore:", rawText, StringComparison.Ordinal);

                return Task.CompletedTask;
            }
        );
    }

    private static List<(string? Name, string? Uses, string? If, string Run)> GetSteps(
        YamlMappingNode root,
        string jobName
    )
    {
        var jobs = GetMapping(root, "jobs");
        var job = GetMapping(jobs, jobName);
        var steps = GetSequence(job, "steps");

        var result = new List<(string? Name, string? Uses, string? If, string Run)>();
        foreach (var stepNode in steps.Children)
        {
            var step = (YamlMappingNode)stepNode;
            var name = TryGetChild(step, "name") is YamlScalarNode nameNode ? nameNode.Value : null;
            var uses = TryGetChild(step, "uses") is YamlScalarNode usesNode ? usesNode.Value : null;
            var ifValue = TryGetChild(step, "if") is YamlScalarNode ifNode ? ifNode.Value : null;
            var runValue = TryGetChild(step, "run") is YamlScalarNode runNode
                ? runNode.Value ?? string.Empty
                : string.Empty;
            result.Add((name, uses, ifValue, runValue));
        }

        return result;
    }

    /// <summary>
    /// Evaluates the subset of GitHub Actions <c>if:</c> expressions used by the workflow without running GitHub Actions.
    /// </summary>
    private static bool EvaluateGithubActionsExpression(
        string expression,
        IReadOnlyDictionary<string, string> context
    )
    {
        foreach (var rawClause in expression.Split("&&"))
        {
            var clause = rawClause.Trim();
            var negate = clause.StartsWith('!');
            if (negate)
            {
                clause = clause[1..].Trim();
            }

            bool clauseResult;
            var containsMatch = Regex.Match(
                clause,
                @"^contains\((?<expr>[^,]+),\s*'(?<value>[^']*)'\)$"
            );
            if (containsMatch.Success)
            {
                var left = ResolveGithubActionsExpressionValue(
                    containsMatch.Groups["expr"].Value.Trim(),
                    context
                );
                clauseResult = left.Contains(
                    containsMatch.Groups["value"].Value,
                    StringComparison.Ordinal
                );
            }
            else
            {
                var comparisonMatch = Regex.Match(
                    clause,
                    @"^(?<left>[A-Za-z0-9_.]+)\s*(?<op>==|!=)\s*'(?<value>[^']*)'$"
                );
                if (!comparisonMatch.Success)
                {
                    throw new NotSupportedException(
                        $"Unsupported GitHub Actions expression clause: '{clause}'."
                    );
                }

                var left = ResolveGithubActionsExpressionValue(
                    comparisonMatch.Groups["left"].Value,
                    context
                );
                var equal = string.Equals(
                    left,
                    comparisonMatch.Groups["value"].Value,
                    StringComparison.Ordinal
                );
                clauseResult = comparisonMatch.Groups["op"].Value == "==" ? equal : !equal;
            }

            if (negate)
            {
                clauseResult = !clauseResult;
            }

            if (!clauseResult)
            {
                return false;
            }
        }

        return true;
    }

    private static string ResolveGithubActionsExpressionValue(
        string reference,
        IReadOnlyDictionary<string, string> context
    )
    {
        var normalized = reference.Replace(
            "github.event.inputs.",
            "inputs.",
            StringComparison.Ordinal
        );
        return context.TryGetValue(normalized, out var value) ? value : string.Empty;
    }

    private static string GetCiWorkflowPath(string outputDirectory) =>
        Path.Combine(outputDirectory, ".github", "workflows", "ci.yml");

    private static string ReadCiWorkflowRawText(string outputDirectory) =>
        File.ReadAllText(GetCiWorkflowPath(outputDirectory));

    private static YamlMappingNode LoadCiWorkflowRoot(string outputDirectory) =>
        LoadYamlMappingRoot(GetCiWorkflowPath(outputDirectory));

    private static YamlMappingNode LoadYamlMappingRoot(string path)
    {
        Assert.True(File.Exists(path), $"Expected YAML file at '{path}'.");

        var yaml = new YamlStream();
        using var reader = new StringReader(File.ReadAllText(path));
        yaml.Load(reader);
        return (YamlMappingNode)yaml.Documents[0].RootNode;
    }

    /// <summary>Reads the <c>exporters</c> sequence of a collector pipeline (traces/logs/metrics).</summary>
    private static List<string?> GetExporterNames(YamlMappingNode pipelines, string pipelineName) =>
        GetSequence(GetMapping(pipelines, pipelineName), "exporters")
            .Children.Select(node => ((YamlScalarNode)node).Value)
            .ToList();

    private static YamlNode? TryGetChild(YamlMappingNode node, string key) =>
        node.Children.TryGetValue(new YamlScalarNode(key), out var value) ? value : null;

    private static YamlMappingNode GetMapping(YamlMappingNode node, string key)
    {
        var child = TryGetChild(node, key);
        Assert.NotNull(child);
        return Assert.IsType<YamlMappingNode>(child);
    }

    private static YamlSequenceNode GetSequence(YamlMappingNode node, string key)
    {
        var child = TryGetChild(node, key);
        Assert.NotNull(child);
        return Assert.IsType<YamlSequenceNode>(child);
    }

    private static string GetScalar(YamlMappingNode node, string key)
    {
        var child = TryGetChild(node, key);
        Assert.NotNull(child);
        return Assert.IsType<YamlScalarNode>(child).Value ?? string.Empty;
    }

    private static JsonDocument ReadJsonFile(string outputDirectory, params string[] relativePath)
    {
        var path = Path.Combine([outputDirectory, .. relativePath]);
        Assert.True(File.Exists(path), $"Expected generated file at '{path}'.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static async Task<string> GenerateWebApiProjectAsync(
        string projectName,
        string? orm = null,
        string? orchestrator = null,
        string? databaseProvider = null
    )
    {
        var extraArgs = new List<string>();
        if (orm is not null)
        {
            extraArgs.Add("--Orm");
            extraArgs.Add(orm);
        }
        if (orchestrator is not null)
        {
            extraArgs.Add("--Orchestrator");
            extraArgs.Add(orchestrator);
        }
        if (databaseProvider is not null)
        {
            extraArgs.Add("--DatabaseProvider");
            extraArgs.Add(databaseProvider);
        }

        var outputDirectory = Path.Combine(
            BuildSupport.RealTempRoot,
            $"dorn-ci-tests-webapi-{Guid.NewGuid():N}"
        );
        await WebApiTemplateGenerationTests.GenerateAsync(
            projectName,
            outputDirectory,
            extraArgs.ToArray()
        );
        return outputDirectory;
    }

    private static async Task WithGeneratedWebApiProjectAsync(
        string projectName,
        Func<string, Task> body,
        string? orm = null,
        string? orchestrator = null,
        string? databaseProvider = null
    )
    {
        var outputDirectory = await GenerateWebApiProjectAsync(
            projectName,
            orm,
            orchestrator,
            databaseProvider
        );
        try
        {
            await body(outputDirectory);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                await BuildSupport.DeleteDirectoryWithRetryAsync(outputDirectory);
            }
        }
    }
}

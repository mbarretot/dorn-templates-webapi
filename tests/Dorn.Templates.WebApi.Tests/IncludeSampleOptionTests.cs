using Xunit;

namespace Dorn.Templates.WebApi.Tests;

/// <summary>APP-14: the <c>IncludeSample</c> template parameter (the Todo sample feature).</summary>
[Trait("Category", "Integration")]
[Collection(TemplatePackCollection.Name)]
public class IncludeSampleOptionTests
{
    private static readonly string[] Tiers =
    [
        "Application",
        "Integration",
        "Architecture",
        "Functional",
    ];

    [Fact]
    public async Task Generate_ByDefault_IncludesTheTodoSample()
    {
        const string name = "DornSampleDefaultApp";
        await GeneratedProject.WithAsync(
            name,
            async outputDirectory =>
            {
                Assert.True(
                    File.Exists(
                        GeneratedProject.SourcePath(
                            outputDirectory,
                            $"{name}.Domain",
                            "Entities",
                            "TodoItem.cs"
                        )
                    )
                );
                Assert.True(
                    File.Exists(
                        GeneratedProject.SourcePath(
                            outputDirectory,
                            $"{name}.WebApi",
                            "Endpoints",
                            "TodoEndpoints.cs"
                        )
                    )
                );
                Assert.True(
                    Directory.Exists(
                        GeneratedProject.SourcePath(
                            outputDirectory,
                            $"{name}.Infrastructure",
                            "Persistence",
                            "Migrations"
                        )
                    )
                );
                Assert.True(
                    Directory.Exists(
                        GeneratedProject.TestPath(
                            outputDirectory,
                            $"{name}.Functional.Tests",
                            "Todos"
                        )
                    )
                );
                var program = await File.ReadAllTextAsync(
                    GeneratedProject.SourcePath(outputDirectory, $"{name}.WebApi", "Program.cs")
                );
                Assert.Contains("MapTodoEndpoints", program, StringComparison.Ordinal);
            }
        );
    }

    [Theory]
    [InlineData("efcore", "sqlite", "aspire", "none")]
    [InlineData("efcore", "sqlserver", "docker-compose", "azure-ad")]
    [InlineData("efcore", "postgres", "none", "none")]
    [InlineData("dapper", "sqlite", "none", "azure-ad")]
    [InlineData("dapper", "sqlserver", "aspire", "none")]
    [InlineData("dapper", "postgres", "docker-compose", "none")]
    public async Task Generate_WithoutTheSample_LeavesNoTodoBehind(
        string orm,
        string databaseProvider,
        string orchestrator,
        string auth
    )
    {
        const string name = "DornNoSampleApp";
        await GeneratedProject.WithAsync(
            name,
            outputDirectory =>
            {
                var leftovers = Directory
                    .EnumerateFileSystemEntries(outputDirectory, "*", SearchOption.AllDirectories)
                    .Where(path => !IsBuildOutput(path))
                    .Where(path =>
                        Path.GetFileName(path).Contains("Todo", StringComparison.OrdinalIgnoreCase)
                        || (
                            File.Exists(path)
                            && File.ReadAllText(path)
                                .Contains("Todo", StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .Select(path => Path.GetRelativePath(outputDirectory, path))
                    .ToList();
                Assert.True(
                    leftovers.Count == 0,
                    $"IncludeSample=false must leave no Todo behind, found: {string.Join(", ", leftovers)}"
                );

                Assert.False(
                    Directory.Exists(
                        GeneratedProject.SourcePath(
                            outputDirectory,
                            $"{name}.Infrastructure",
                            "Persistence",
                            "Migrations"
                        )
                    ),
                    "The sample owns every migration."
                );
                foreach (var tier in Tiers)
                {
                    var tests = Directory.EnumerateFiles(
                        GeneratedProject.TestPath(outputDirectory, $"{name}.{tier}.Tests"),
                        "*.cs",
                        SearchOption.AllDirectories
                    );
                    Assert.True(
                        tests.Any(file =>
                            !file.EndsWith("GlobalUsings.cs", StringComparison.Ordinal)
                        ),
                        $"The {tier} tier must keep at least one test."
                    );
                }

                return Task.CompletedTask;
            },
            "--IncludeSample",
            "false",
            "--Orm",
            orm,
            "--DatabaseProvider",
            databaseProvider,
            "--Orchestrator",
            orchestrator,
            "--Auth",
            auth
        );
    }

    [Fact]
    public async Task Generate_WithoutTheSample_KeepsTheReplacementTests()
    {
        const string name = "DornNoSampleReplacementApp";
        await GeneratedProject.WithAsync(
            name,
            outputDirectory =>
            {
                Assert.True(
                    File.Exists(
                        GeneratedProject.TestPath(
                            outputDirectory,
                            $"{name}.Integration.Tests",
                            "PersistenceConnectivityTests.cs"
                        )
                    )
                );
                Assert.True(
                    File.Exists(
                        GeneratedProject.TestPath(
                            outputDirectory,
                            $"{name}.Functional.Tests",
                            "ApiSmokeTests.cs"
                        )
                    )
                );
                Assert.True(
                    File.Exists(
                        GeneratedProject.TestPath(
                            outputDirectory,
                            $"{name}.Architecture.Tests",
                            "LayeringTests.cs"
                        )
                    )
                );
                return Task.CompletedTask;
            },
            "--IncludeSample",
            "false"
        );
    }

    [Theory]
    [InlineData("efcore", "sqlite", "aspire")]
    [InlineData("dapper", "sqlite", "none")]
    public async Task GenerateBuildAndTest_WithoutTheSample_RunsAllFourTiers(
        string orm,
        string databaseProvider,
        string orchestrator
    )
    {
        const string name = "DornNoSampleTiersApp";
        await GeneratedProject.WithBuiltSolutionAsync(
            name,
            async (outputDirectory, slnPath) =>
            {
                var testResult = await TemplatePackHarness.RunProcessAsync(
                    outputDirectory,
                    null,
                    "test",
                    slnPath,
                    "-c",
                    "Release",
                    "--no-build"
                );
                Assert.True(
                    testResult.ExitCode == 0,
                    $"Generated tests exited with {testResult.ExitCode}."
                        + $"{Environment.NewLine}STDOUT:{Environment.NewLine}{testResult.StdOut}"
                        + $"{Environment.NewLine}STDERR:{Environment.NewLine}{testResult.StdErr}"
                );
                foreach (var tier in Tiers)
                {
                    Assert.Contains($"{name}.{tier}.Tests", testResult.StdOut);
                }
            },
            "--IncludeSample",
            "false",
            "--Orm",
            orm,
            "--DatabaseProvider",
            databaseProvider,
            "--Orchestrator",
            orchestrator
        );
    }

    [Theory]
    [InlineData("efcore", "sqlserver", "docker-compose", "azure-ad", true)]
    [InlineData("dapper", "postgres", "none", "none", true)]
    [InlineData("efcore", "postgres", "aspire", "none", false)]
    public async Task GenerateAndBuild_WithoutTheSample_ProducesBuildableSolution(
        string orm,
        string databaseProvider,
        string orchestrator,
        string auth,
        bool includeTests
    )
    {
        await GeneratedProject.WithBuiltSolutionAsync(
            "DornNoSampleBuildApp",
            (_, _) => Task.CompletedTask,
            "--IncludeSample",
            "false",
            "--IncludeTests",
            includeTests ? "true" : "false",
            "--Orm",
            orm,
            "--DatabaseProvider",
            databaseProvider,
            "--Orchestrator",
            orchestrator,
            "--Auth",
            auth
        );
    }

    [Fact]
    public async Task GenerateAndBuild_WithoutTheSampleAndCustomAuth_FailsBuildWithActionableError()
    {
        await GeneratedProject.WithSolutionAsync(
            "DornNoSampleCustomAuthApp",
            async (_, slnPath) =>
            {
                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);

                Assert.NotEqual(0, buildResult.ExitCode);
                Assert.Contains(
                    "Auth=custom requires IncludeSample=true",
                    buildResult.StdOut,
                    StringComparison.Ordinal
                );
            },
            "--IncludeSample",
            "false",
            "--Auth",
            "custom",
            "--IncludeTests",
            "false"
        );
    }

    private static bool IsBuildOutput(string path)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        return path.Split(separators).Any(part => part is "bin" or "obj");
    }
}

using System.Text.Json;
using Xunit;

namespace Dorn.Templates.WebApi.Tests;

/// <summary>APP-9: the <c>IncludeLocalization</c>, <c>DefaultLanguage</c>, and <c>Languages</c> template parameters.</summary>
[Trait("Category", "Integration")]
[Collection(TemplatePackCollection.Name)]
public class LocalizationOptionTests
{
    private const string Name = "PolyglotApp";

    private static string ResourcePath(string outputDirectory, string file) =>
        GeneratedProject.SourcePath(outputDirectory, $"{Name}.WebApi", "Localization", file);

    private static IEnumerable<string> Resources(string outputDirectory)
    {
        var directory = GeneratedProject.SourcePath(
            outputDirectory,
            $"{Name}.WebApi",
            "Localization"
        );
        return Directory
            .EnumerateFiles(directory, "*.resx")
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)!;
    }

    [Fact]
    public async Task Generate_ByDefault_LeavesNoLocalizationBehind()
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                var leftovers = new List<string>();
                foreach (
                    var path in Directory.EnumerateFiles(
                        outputDirectory,
                        "*",
                        SearchOption.AllDirectories
                    )
                )
                {
                    var extension = Path.GetExtension(path);
                    if (
                        Path.GetFileName(path)
                            .Contains("Locali", StringComparison.OrdinalIgnoreCase)
                        || extension == ".resx"
                        || (
                            extension
                                is ".cs"
                                    or ".json"
                                    or ".md"
                                    or ".csproj"
                                    or ".yml"
                                    or ".props"
                            && (await File.ReadAllTextAsync(path)).Contains(
                                "Locali",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    {
                        leftovers.Add(Path.GetRelativePath(outputDirectory, path));
                    }
                }

                Assert.True(
                    leftovers.Count == 0,
                    $"IncludeLocalization=false must leave nothing behind, found: {string.Join(", ", leftovers)}"
                );
            }
        );
    }

    [Fact]
    public async Task Generate_WithLocalizationAndDefaults_EmitsTheNeutralResourceAndWiresRequestLocalization()
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                Assert.Equal(["SharedResource.resx"], Resources(outputDirectory));
                Assert.True(File.Exists(ResourcePath(outputDirectory, "SharedResource.cs")));

                var program = await File.ReadAllTextAsync(
                    GeneratedProject.SourcePath(outputDirectory, $"{Name}.WebApi", "Program.cs")
                );
                Assert.Contains("AddApiLocalization", program, StringComparison.Ordinal);
                Assert.Contains("UseApiLocalization", program, StringComparison.Ordinal);
                Assert.True(
                    program.IndexOf("UseApiLocalization", StringComparison.Ordinal)
                        < program.IndexOf("UseExceptionHandler", StringComparison.Ordinal),
                    "Request localization must run before the exception handler so error responses are localized."
                );

                var (defaultCulture, supported) = ReadLocalizationSettings(outputDirectory);
                Assert.Equal("en", defaultCulture);
                Assert.Equal(["en"], supported);

                Assert.True(
                    File.Exists(
                        GeneratedProject.TestPath(
                            outputDirectory,
                            $"{Name}.Functional.Tests",
                            "LocalizationTests.cs"
                        )
                    )
                );
            },
            "--IncludeLocalization",
            "true"
        );
    }

    [Fact]
    public async Task Generate_WithLanguages_EmitsOneResourcePerLanguageExceptEnglish()
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                Assert.Equal(
                    [
                        "SharedResource.es.resx",
                        "SharedResource.gl.resx",
                        "SharedResource.pt-BR.resx",
                        "SharedResource.resx",
                        "SharedResource.zh-Hans.resx",
                    ],
                    Resources(outputDirectory)
                );

                var neutral = await File.ReadAllTextAsync(
                    ResourcePath(outputDirectory, "SharedResource.resx")
                );
                var galician = await File.ReadAllTextAsync(
                    ResourcePath(outputDirectory, "SharedResource.gl.resx")
                );
                Assert.Contains("ValidationProblem.Title", neutral, StringComparison.Ordinal);
                Assert.Contains("ValidationProblem.Title", galician, StringComparison.Ordinal);

                var (defaultCulture, supported) = ReadLocalizationSettings(outputDirectory);
                Assert.Equal("es", defaultCulture);
                Assert.Equal(["es", "en", "pt-BR", "gl", "zh-Hans"], supported);
            },
            "--IncludeLocalization",
            "true",
            "--DefaultLanguage",
            "es",
            "--Languages",
            "es, en,pt-BR ,gl,zh-Hans"
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Generate_WithAgentRules_MentionsLocalizationOnlyWhenItIsGenerated(
        bool includeLocalization
    )
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                var agents = await File.ReadAllTextAsync(
                    Path.Combine(outputDirectory, "AGENTS.md")
                );
                Assert.Equal(
                    includeLocalization,
                    agents.Contains("SharedResource", StringComparison.Ordinal)
                );
                Assert.DoesNotContain("<!--#", agents, StringComparison.Ordinal);
            },
            "--IncludeAgentRules",
            "true",
            "--IncludeLocalization",
            includeLocalization ? "true" : "false"
        );
    }

    [Fact]
    public async Task Generate_WithoutLocalization_IgnoresLanguageParameters()
    {
        await GeneratedProject.WithAsync(
            Name,
            outputDirectory =>
            {
                Assert.False(
                    Directory.Exists(
                        GeneratedProject.SourcePath(
                            outputDirectory,
                            $"{Name}.WebApi",
                            "Localization"
                        )
                    )
                );
                return Task.CompletedTask;
            },
            "--DefaultLanguage",
            "es",
            "--Languages",
            "es,pt-BR"
        );
    }

    [Theory]
    [InlineData("--Languages", "es,not a code")]
    [InlineData("--Languages", "../evil")]
    [InlineData("--Languages", "es;pt")]
    [InlineData("--Languages", "e")]
    [InlineData("--Languages", "es,ES")]
    [InlineData("--Languages", "es,,pt")]
    [InlineData("--DefaultLanguage", "xx_yy")]
    [InlineData("--DefaultLanguage", "en us")]
    [InlineData(
        "--Languages",
        "aa,ab,ac,ad,ae,af,ag,ah,ai,aj,ak,al,am,an,ao,ap,aq,ar,as,at,au,av,aw,ax,ay"
    )]
    public async Task Generate_WithAnInvalidLanguageSetting_FailsTheBuildWithoutWritingOutsideTheProject(
        string option,
        string value
    )
    {
        await GeneratedProject.WithSolutionAsync(
            Name,
            async (outputDirectory, _) =>
            {
                Assert.False(
                    File.Exists(Path.Combine(Path.GetDirectoryName(outputDirectory)!, "evil.resx"))
                );
                Assert.DoesNotContain(
                    Resources(outputDirectory),
                    file => file.Contains("evil", StringComparison.Ordinal)
                );

                var build = await TemplatePackHarness.RunProcessAsync(
                    outputDirectory,
                    null,
                    "build",
                    GeneratedProject.SourcePath(
                        outputDirectory,
                        $"{Name}.Domain",
                        $"{Name}.Domain.csproj"
                    ),
                    "-nodeReuse:false"
                );

                Assert.NotEqual(0, build.ExitCode);
                Assert.Contains("IncludeLocalization", build.StdOut, StringComparison.Ordinal);
            },
            "--IncludeLocalization",
            "true",
            option,
            value
        );
    }

    [Theory]
    [InlineData("efcore", "sqlite", "aspire", "none", true)]
    [InlineData("dapper", "sqlite", "none", "none", true)]
    [InlineData("efcore", "sqlite", "docker-compose", "custom", true)]
    [InlineData("dapper", "sqlite", "none", "none", false)]
    public async Task GenerateBuildAndTest_WithLocalization_RunsAllFourTiers(
        string orm,
        string databaseProvider,
        string orchestrator,
        string auth,
        bool includeSample
    )
    {
        await GeneratedProject.WithBuiltSolutionAsync(
            Name,
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
                Assert.Contains($"{Name}.Functional.Tests", testResult.StdOut);
            },
            "--IncludeLocalization",
            "true",
            "--DefaultLanguage",
            "es",
            "--Languages",
            "es,pt-BR,gl",
            "--IncludeSample",
            includeSample ? "true" : "false",
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

    [Theory]
    [InlineData("efcore", "sqlserver", "docker-compose", "azure-ad")]
    [InlineData("efcore", "postgres", "aspire", "custom")]
    [InlineData("dapper", "postgres", "none", "none")]
    public async Task GenerateAndBuild_WithLocalization_ProducesBuildableSolution(
        string orm,
        string databaseProvider,
        string orchestrator,
        string auth
    )
    {
        await GeneratedProject.WithBuiltSolutionAsync(
            Name,
            (_, _) => Task.CompletedTask,
            "--IncludeLocalization",
            "true",
            "--Languages",
            "en,fr",
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

    private static (string DefaultCulture, string[] Supported) ReadLocalizationSettings(
        string outputDirectory
    )
    {
        using var settings = GeneratedProject.ReadJson(
            outputDirectory,
            "src",
            $"{Name}.WebApi",
            "appsettings.json"
        );
        var section = settings.RootElement.GetProperty("Localization");
        return (
            section.GetProperty("DefaultCulture").GetString()!,
            section
                .GetProperty("SupportedCultures")
                .EnumerateArray()
                .Select(element => element.GetString()!)
                .ToArray()
        );
    }
}

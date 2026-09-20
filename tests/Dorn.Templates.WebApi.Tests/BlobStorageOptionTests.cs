using System.Text.RegularExpressions;
using Xunit;

namespace Dorn.Templates.WebApi.Tests;

/// <summary>APP-10/APP-11: the <c>BlobStorage</c> template parameter (<c>manual</c>, <c>database</c>, <c>filesystem</c>).</summary>
[Trait("Category", "Integration")]
[Collection(TemplatePackCollection.Name)]
public class BlobStorageOptionTests
{
    private const string Name = "AttachmentsApp";

    // Not a bare "blob": generated READMEs link to github.com/.../blob/main.
    private static readonly string[] BlobTokens =
        ["IBlobStore", "BlobStorage", "BlobRecord", "BlobRules", "Blobs"];

    private static string Infrastructure(string outputDirectory, params string[] rest) =>
        GeneratedProject.SourcePath(outputDirectory, $"{Name}.Infrastructure", rest);

    private static string Storage(string outputDirectory, string file) =>
        Infrastructure(outputDirectory, "Storage", file);

    private static string Migrations(string outputDirectory) =>
        Infrastructure(outputDirectory, "Persistence", "Migrations");

    private static async Task<string> ReadAsync(string path)
    {
        Assert.True(File.Exists(path), $"Expected generated file at '{path}'.");
        return await File.ReadAllTextAsync(path);
    }

    private static string WithoutMigrationTimestamp(string path) =>
        Regex.Replace(path, @"\d{14}_AddBlobs", "AddBlobs");

    private static IEnumerable<string> BlobFiles(string outputDirectory) =>
        Directory
            .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Where(path =>
                Path.GetFileName(path).Contains("Blob", StringComparison.OrdinalIgnoreCase)
            )
            .Select(path => Path.GetRelativePath(outputDirectory, path))
            .Order(StringComparer.Ordinal);

    [Fact]
    public async Task Generate_ByDefault_LeavesNoBlobStorageBehind()
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                var leftovers = new List<string>(BlobFiles(outputDirectory));
                foreach (
                    var path in Directory
                        .EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
                        .Where(path =>
                            Path.GetExtension(path)
                                is ".cs"
                                    or ".json"
                                    or ".md"
                                    or ".csproj"
                                    or ".yml"
                                    or ".props"
                        )
                )
                {
                    var text = await File.ReadAllTextAsync(path);
                    if (
                        BlobTokens.Any(token =>
                            text.Contains(token, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        leftovers.Add(Path.GetRelativePath(outputDirectory, path));
                    }
                }

                Assert.True(
                    leftovers.Count == 0,
                    $"BlobStorage=manual must leave nothing behind, found: {string.Join(", ", leftovers)}"
                );
            }
        );
    }

    [Fact]
    public async Task Generate_WithManual_ProducesExactlyTheDefaultSolution()
    {
        await GeneratedProject.WithAsync(
            Name,
            async defaultDirectory =>
            {
                await GeneratedProject.WithAsync(
                    Name,
                    async manualDirectory =>
                    {
                        var defaultFiles = RelativeFiles(defaultDirectory);
                        Assert.Equal(defaultFiles, RelativeFiles(manualDirectory));
                        foreach (var file in defaultFiles)
                        {
                            var expected = await File.ReadAllBytesAsync(
                                Path.Combine(defaultDirectory, file)
                            );
                            var actual = await File.ReadAllBytesAsync(
                                Path.Combine(manualDirectory, file)
                            );
                            Assert.True(
                                expected.SequenceEqual(actual),
                                $"'{file}' differs between the default and BlobStorage=manual."
                            );
                        }
                    },
                    "--BlobStorage",
                    "manual"
                );
            }
        );

        static string[] RelativeFiles(string root) =>
            Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path))
                .Order(StringComparer.Ordinal)
                .ToArray();
    }

    [Theory]
    [InlineData("efcore")]
    [InlineData("dapper")]
    public async Task Generate_WithFileSystem_EmitsThePortTheStoreAndTheConfigurationOnly(string orm)
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                Assert.Equal(
                    [
                        Path.Combine(
                            "src",
                            $"{Name}.Application",
                            "Common",
                            "Storage",
                            "BlobRules.cs"
                        ),
                        Path.Combine(
                            "src",
                            $"{Name}.Application",
                            "Common",
                            "Storage",
                            "IBlobStore.cs"
                        ),
                        Path.Combine(
                            "src",
                            $"{Name}.Infrastructure",
                            "Storage",
                            "FileSystemBlobStore.cs"
                        ),
                        Path.Combine(
                            "tests",
                            $"{Name}.Application.Tests",
                            "Common",
                            "Storage",
                            "BlobRulesTests.cs"
                        ),
                        Path.Combine(
                            "tests",
                            $"{Name}.Functional.Tests",
                            "BlobStorageTests.cs"
                        ),
                        Path.Combine(
                            "tests",
                            $"{Name}.Integration.Tests",
                            "Storage",
                            "FileSystemBlobStoreTests.cs"
                        ),
                    ],
                    BlobFiles(outputDirectory)
                );

                using var settings = GeneratedProject.ReadJson(
                    outputDirectory,
                    "src",
                    $"{Name}.WebApi",
                    "appsettings.json"
                );
                Assert.Equal(
                    "App_Data/blobs",
                    settings
                        .RootElement.GetProperty("BlobStorage")
                        .GetProperty("FileSystem")
                        .GetProperty("RootPath")
                        .GetString()
                );

                var registration = await ReadAsync(
                    Infrastructure(
                        outputDirectory,
                        "DependencyInjection",
                        "ServiceCollectionExtensions.cs"
                    )
                );
                Assert.Contains("FileSystemBlobStore", registration, StringComparison.Ordinal);
                Assert.DoesNotContain("<!--#", registration, StringComparison.Ordinal);
            },
            "--BlobStorage",
            "filesystem",
            "--Orm",
            orm
        );
    }

    [Theory]
    [InlineData("sqlite", "BLOB")]
    [InlineData("sqlserver", "varbinary(max)")]
    [InlineData("postgres", "bytea")]
    public async Task Generate_WithDatabaseAndEfCore_ShipsTheBlobMigrationForTheSelectedProvider(
        string databaseProvider,
        string binaryColumnType
    )
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                var infrastructure = Infrastructure(outputDirectory);
                Assert.Equal(
                    [
                        Path.Combine("Persistence", "Migrations", "AddBlobs.Designer.cs"),
                        Path.Combine("Persistence", "Migrations", "AddBlobs.cs"),
                        Path.Combine("Storage", "BlobRecord.cs"),
                        Path.Combine("Storage", "EfCoreBlobStore.cs"),
                    ],
                    Directory
                        .EnumerateFiles(infrastructure, "*", SearchOption.AllDirectories)
                        .Select(path => Path.GetRelativePath(infrastructure, path))
                        .Select(WithoutMigrationTimestamp)
                        .Where(path => path.Contains("Blob", StringComparison.Ordinal))
                        .Order(StringComparer.Ordinal)
                );

                var migration = await ReadAsync(
                    Directory
                        .EnumerateFiles(Migrations(outputDirectory), "*_AddBlobs.cs")
                        .Single()
                );
                Assert.Contains(
                    $"type: \"{binaryColumnType}\"",
                    migration,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain("#if", migration, StringComparison.Ordinal);

                var snapshot = await ReadAsync(
                    Path.Combine(Migrations(outputDirectory), "ApplicationDbContextModelSnapshot.cs")
                );
                Assert.Contains("BlobRecord", snapshot, StringComparison.Ordinal);
                Assert.DoesNotContain("#if", snapshot, StringComparison.Ordinal);

                var context = await ReadAsync(
                    Infrastructure(outputDirectory, "Persistence", "ApplicationDbContext.cs")
                );
                Assert.Contains("Entity<BlobRecord>", context, StringComparison.Ordinal);

                using var settings = GeneratedProject.ReadJson(
                    outputDirectory,
                    "src",
                    $"{Name}.WebApi",
                    "appsettings.json"
                );
                Assert.False(settings.RootElement.TryGetProperty("BlobStorage", out _));
            },
            "--BlobStorage",
            "database",
            "--DatabaseProvider",
            databaseProvider
        );
    }

    [Theory]
    [InlineData("sqlite", "BLOB NOT NULL")]
    [InlineData("sqlserver", "VARBINARY(MAX) NOT NULL")]
    [InlineData("postgres", "BYTEA NOT NULL")]
    public async Task Generate_WithDatabaseAndDapper_BootstrapsTheBlobsTableEvenWithoutTheSample(
        string databaseProvider,
        string binaryColumnType
    )
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                Assert.False(Directory.Exists(Migrations(outputDirectory)));
                Assert.False(File.Exists(Storage(outputDirectory, "EfCoreBlobStore.cs")));
                Assert.False(File.Exists(Storage(outputDirectory, "BlobRecord.cs")));
                Assert.True(File.Exists(Storage(outputDirectory, "DapperBlobStore.cs")));

                var context = await ReadAsync(
                    Infrastructure(outputDirectory, "Repositories", "Dapper", "DapperContext.cs")
                );
                Assert.Contains(binaryColumnType, context, StringComparison.Ordinal);
                Assert.DoesNotContain("TodoItems", context, StringComparison.Ordinal);

                var program = await ReadAsync(
                    GeneratedProject.SourcePath(outputDirectory, $"{Name}.WebApi", "Program.cs")
                );
                Assert.Contains("InitializeSchemaAsync", program, StringComparison.Ordinal);
            },
            "--BlobStorage",
            "database",
            "--Orm",
            "dapper",
            "--DatabaseProvider",
            databaseProvider,
            "--IncludeSample",
            "false"
        );
    }

    [Fact]
    public async Task Generate_WithDatabaseEfCoreAndNoSample_ShipsOnlyTheBlobMigration()
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                Assert.Equal(
                    ["AddBlobs.Designer.cs", "AddBlobs.cs", "ApplicationDbContextModelSnapshot.cs"],
                    Directory
                        .EnumerateFiles(Migrations(outputDirectory))
                        .Select(path => WithoutMigrationTimestamp(Path.GetFileName(path)))
                        .Order(StringComparer.Ordinal)
                );

                var snapshot = await ReadAsync(
                    Path.Combine(Migrations(outputDirectory), "ApplicationDbContextModelSnapshot.cs")
                );
                Assert.Contains("BlobRecord", snapshot, StringComparison.Ordinal);
                Assert.DoesNotContain("TodoItem", snapshot, StringComparison.Ordinal);
            },
            "--BlobStorage",
            "database",
            "--IncludeSample",
            "false"
        );
    }

    [Theory]
    [InlineData("database", true)]
    [InlineData("filesystem", true)]
    [InlineData("manual", false)]
    public async Task Generate_WithAgentRules_MentionsBlobStorageOnlyWhenItIsGenerated(
        string blobStorage,
        bool mentioned
    )
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                var agents = await File.ReadAllTextAsync(
                    Path.Combine(outputDirectory, "AGENTS.md")
                );
                Assert.Equal(mentioned, agents.Contains("IBlobStore", StringComparison.Ordinal));
                Assert.DoesNotContain("<!--#", agents, StringComparison.Ordinal);
            },
            "--IncludeAgentRules",
            "true",
            "--BlobStorage",
            blobStorage
        );
    }

    [Theory]
    [InlineData("sqlite", "filesystem", true)]
    [InlineData("postgres", "filesystem", true)]
    [InlineData("sqlserver", "database", false)]
    public async Task Generate_WithDockerCompose_MountsAVolumeForFileSystemBlobsOnly(
        string databaseProvider,
        string blobStorage,
        bool mounted
    )
    {
        await GeneratedProject.WithAsync(
            Name,
            async outputDirectory =>
            {
                var compose = await File.ReadAllTextAsync(
                    Path.Combine(outputDirectory, "docker-compose.yml")
                );
                Assert.Equal(
                    mounted,
                    compose.Contains("blobs:/data/blobs", StringComparison.Ordinal)
                );
                Assert.Equal(
                    mounted,
                    compose.Contains(
                        "BlobStorage__FileSystem__RootPath=/data/blobs",
                        StringComparison.Ordinal
                    )
                );
                Assert.DoesNotContain("#if", compose, StringComparison.Ordinal);
            },
            "--Orchestrator",
            "docker-compose",
            "--DatabaseProvider",
            databaseProvider,
            "--BlobStorage",
            blobStorage
        );
    }

    [Fact]
    public async Task Generate_WithAnUnknownChoice_IsRejectedAndListsTheValidChoices()
    {
        var outputDirectory = Path.Combine(
            BuildSupport.RealTempRoot,
            $"dorn-tests-webapi-{Guid.NewGuid():N}"
        );
        try
        {
            var result = await TemplatePackHarness.GenerateAsync(
                "dorn-webapi",
                Name,
                outputDirectory,
                "--BlobStorage",
                "s3"
            );

            Assert.NotEqual(0, result.ExitCode);
            var output = result.StdOut + result.StdErr;
            Assert.Contains("BlobStorage", output, StringComparison.Ordinal);
            Assert.Contains("manual", output, StringComparison.Ordinal);
            Assert.Contains("database", output, StringComparison.Ordinal);
            Assert.Contains("filesystem", output, StringComparison.Ordinal);
            Assert.False(Directory.Exists(outputDirectory));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                await BuildSupport.DeleteDirectoryWithRetryAsync(outputDirectory);
            }
        }
    }

    [Theory]
    [InlineData("efcore", "database", "aspire", "none", true)]
    [InlineData("dapper", "database", "none", "none", true)]
    [InlineData("efcore", "filesystem", "none", "none", true)]
    [InlineData("dapper", "filesystem", "none", "none", false)]
    [InlineData("dapper", "database", "none", "none", false)]
    [InlineData("efcore", "database", "docker-compose", "custom", true)]
    public async Task GenerateBuildAndTest_WithBlobStorage_RunsAllFourTiers(
        string orm,
        string blobStorage,
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
                Assert.Contains($"{Name}.Integration.Tests", testResult.StdOut);
                Assert.Contains($"{Name}.Functional.Tests", testResult.StdOut);
            },
            "--BlobStorage",
            blobStorage,
            "--IncludeSample",
            includeSample ? "true" : "false",
            "--Orm",
            orm,
            "--Orchestrator",
            orchestrator,
            "--Auth",
            auth
        );
    }

    [Theory]
    [InlineData("efcore", "sqlserver", "database", "docker-compose", "azure-ad")]
    [InlineData("efcore", "postgres", "database", "aspire", "custom")]
    [InlineData("dapper", "postgres", "database", "none", "none")]
    [InlineData("dapper", "sqlserver", "filesystem", "aspire", "none")]
    public async Task GenerateAndBuild_WithBlobStorageOnARealProvider_ProducesBuildableSolution(
        string orm,
        string databaseProvider,
        string blobStorage,
        string orchestrator,
        string auth
    )
    {
        await GeneratedProject.WithBuiltSolutionAsync(
            Name,
            (_, _) => Task.CompletedTask,
            "--BlobStorage",
            blobStorage,
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
}

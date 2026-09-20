using System.Text.Json;
using Xunit;

namespace Dorn.Templates.WebApi.Tests;

internal static class GeneratedProject
{
    public static async Task WithAsync(
        string name,
        Func<string, Task> body,
        params string[] templateArgs
    )
    {
        await WithSolutionAsync(name, (outputDirectory, _) => body(outputDirectory), templateArgs);
    }

    public static async Task WithSolutionAsync(
        string name,
        Func<string, string, Task> body,
        params string[] templateArgs
    )
    {
        var outputDirectory = Path.Combine(
            BuildSupport.RealTempRoot,
            $"dorn-tests-webapi-{Guid.NewGuid():N}"
        );
        try
        {
            var slnPath = await WebApiTemplateGenerationTests.GenerateAsync(
                name,
                outputDirectory,
                templateArgs
            );
            await body(outputDirectory, slnPath);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                await BuildSupport.DeleteDirectoryWithRetryAsync(outputDirectory);
            }
        }
    }

    public static async Task WithBuiltSolutionAsync(
        string name,
        Func<string, string, Task> body,
        params string[] templateArgs
    )
    {
        await WithSolutionAsync(
            name,
            async (outputDirectory, slnPath) =>
            {
                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                Assert.True(
                    buildResult.ExitCode == 0,
                    $"dotnet build exited with {buildResult.ExitCode}."
                        + $"{Environment.NewLine}STDOUT:{Environment.NewLine}{buildResult.StdOut}"
                        + $"{Environment.NewLine}STDERR:{Environment.NewLine}{buildResult.StdErr}"
                );
                await body(outputDirectory, slnPath);
            },
            templateArgs
        );
    }

    public static JsonDocument ReadJson(string outputDirectory, params string[] relativePath)
    {
        var path = Path.Combine([outputDirectory, .. relativePath]);
        Assert.True(File.Exists(path), $"Expected generated file at '{path}'.");
        return JsonDocument.Parse(
            File.ReadAllText(path),
            new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            }
        );
    }

    public static string SourcePath(string outputDirectory, string project, params string[] rest) =>
        Path.Combine([outputDirectory, "src", project, .. rest]);

    public static string TestPath(string outputDirectory, string project, params string[] rest) =>
        Path.Combine([outputDirectory, "tests", project, .. rest]);
}

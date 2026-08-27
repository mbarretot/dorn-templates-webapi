using System.Diagnostics;

namespace Dorn.Templates.WebApi.Tests;

internal static class TemplatePackHarness
{
    public static string RepoRoot { get; } = ResolveRepoRoot();

    public static string TemplatesRoot => Path.Combine(RepoRoot, "templates", "webapi");

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "DornTemplatesWebApi.slnx")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate DornTemplatesWebApi.slnx above '{AppContext.BaseDirectory}'."
        );
    }

    public static async Task<string> PackAsync(string packageId, string outputDirectory)
    {
        var csprojPath = Path.Combine(
            RepoRoot,
            "eng",
            "packaging",
            packageId,
            $"{packageId}.csproj"
        );
        var result = await RunProcessAsync(
            RepoRoot,
            null,
            "pack",
            csprojPath,
            "-c",
            "Release",
            "-p:PackageVersion=0.0.1-test",
            "-o",
            outputDirectory
        );
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet pack failed for {packageId}.{Environment.NewLine}"
                    + $"STDOUT:{Environment.NewLine}{result.StdOut}"
                    + $"{Environment.NewLine}STDERR:{Environment.NewLine}{result.StdErr}"
            );
        }

        return Directory
                .GetFiles(outputDirectory, $"{packageId}.*.nupkg", SearchOption.TopDirectoryOnly)
                .SingleOrDefault()
            ?? throw new FileNotFoundException(
                $"No nupkg produced for {packageId} in '{outputDirectory}'."
            );
    }

    public static async Task InstallAsync(string packageId)
    {
        await UninstallAsync(packageId);
        var packDirectory = Path.Combine(
            Path.GetTempPath(),
            $"dorn-templates-webapi-install-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(packDirectory);
        var nupkgPath = await PackAsync(packageId, packDirectory);

        var result = await RunProcessAsync(RepoRoot, null, "new", "install", nupkgPath);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet new install failed for {packageId}.{Environment.NewLine}"
                    + $"STDOUT:{Environment.NewLine}{result.StdOut}"
                    + $"{Environment.NewLine}STDERR:{Environment.NewLine}{result.StdErr}"
            );
        }
    }

    public static async Task UninstallAsync(string packageId)
    {
        await RunProcessAsync(RepoRoot, null, "new", "uninstall", packageId);
    }

    public static async Task<(int ExitCode, string StdOut, string StdErr)> GenerateAsync(
        string shortName,
        string name,
        string outputDir,
        params string[] extraArgs
    )
    {
        var arguments = new List<string> { "new", shortName, "-n", name, "-o", outputDir };
        arguments.AddRange(extraArgs);
        return await RunProcessAsync(RepoRoot, null, arguments.ToArray());
    }

    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string workingDirectory,
        Dictionary<string, string?>? environment,
        params string[] arguments
    )
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the nested dotnet process.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }
}

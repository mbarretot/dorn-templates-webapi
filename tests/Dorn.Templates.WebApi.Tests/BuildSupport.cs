namespace Dorn.Templates.WebApi.Tests;

internal static class BuildSupport
{
    public static readonly string RealTempRoot = ResolveRealPath(Path.GetTempPath());

    private static string ResolveRealPath(string path)
    {
        var original = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(path);
            return Directory.GetCurrentDirectory();
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
        }
    }

    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunDotnetBuildAsync(
        string solutionPath
    )
    {
        var restoreResult = await RestoreWithRetryAsync(solutionPath);
        if (restoreResult.ExitCode != 0)
        {
            return restoreResult;
        }

        return await TemplatePackHarness.RunProcessAsync(
            Path.GetDirectoryName(solutionPath)!,
            null,
            "build",
            solutionPath,
            "-c",
            "Release",
            "--no-restore",
            "-nodeReuse:false"
        );
    }

    /// <summary>
    /// Retries restore only for the known concurrent generated-file race; other failures return immediately.
    /// </summary>
    private static async Task<(int ExitCode, string StdOut, string StdErr)> RestoreWithRetryAsync(
        string solutionPath,
        int maxAttempts = 3
    )
    {
        (int ExitCode, string StdOut, string StdErr) result = (1, "", "");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            result = await TemplatePackHarness.RunProcessAsync(
                Path.GetDirectoryName(solutionPath)!,
                null,
                "restore",
                solutionPath,
                "-nodeReuse:false",
                "-maxCpuCount:1"
            );

            if (result.ExitCode == 0)
            {
                return result;
            }

            var isKnownRace =
                result.StdOut.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                || result.StdErr.Contains("already exists", StringComparison.OrdinalIgnoreCase);
            if (!isKnownRace || attempt == maxAttempts)
            {
                return result;
            }
        }

        return result;
    }

    // Windows can briefly hold a handle on a just-exited child process's files.
    public static async Task DeleteDirectoryWithRetryAsync(string path)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 5)
            {
                await Task.Delay(200);
            }
        }
    }
}

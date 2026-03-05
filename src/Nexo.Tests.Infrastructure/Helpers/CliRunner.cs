using System.Diagnostics;

namespace Nexo.Tests.Infrastructure.Helpers;

/// <summary>
/// Shared helper for running the Nexo CLI in E2E tests.
/// Consolidates build-once and run logic from Phases14CliE2ETests and similar tests.
/// </summary>
public static class CliRunner
{
    private static readonly object _cliBuildLock = new();
    private static string? _cachedCliPath;

    /// <summary>
    /// Runs the prebuilt Nexo CLI with the given arguments.
    /// </summary>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="args">CLI arguments (e.g. "adapt --dry-run").</param>
    /// <param name="envOverrides">Optional environment variable overrides (e.g. for test isolation).</param>
    /// <param name="timeout">Optional timeout. When provided, cancels and kills the process if it exceeds the limit.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>Exit code, stdout, and stderr.</returns>
    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string workingDir,
        string args,
        IReadOnlyDictionary<string, string?>? envOverrides = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        using var timeoutCts = timeout.HasValue ? CancellationTokenSource.CreateLinkedTokenSource(ct) : null;
        if (timeout.HasValue && timeoutCts != null)
            timeoutCts.CancelAfter(timeout.Value);

        var effectiveCt = timeoutCts?.Token ?? ct;

        var cliPath = await EnsureCliBuiltAsync(workingDir);
        var fullArgs = $"\"{cliPath}\" {args}";

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = fullArgs,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (envOverrides != null)
        {
            foreach (var (k, v) in envOverrides)
            {
                if (v == null)
                    psi.Environment.Remove(k);
                else
                    psi.Environment[k] = v;
            }
        }

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process");
        var stdoutTask = p.StandardOutput.ReadToEndAsync(effectiveCt);
        var stderrTask = p.StandardError.ReadToEndAsync(effectiveCt);

        try
        {
            await p.WaitForExitAsync(effectiveCt);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
            await Task.WhenAny(stdoutTask, stderrTask, Task.Delay(2000));
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (p.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Ensures the Nexo CLI is built and returns the path to Nexo.CLI.dll.
    /// Builds once and caches the path for subsequent calls.
    /// </summary>
    /// <param name="repoRoot">Repository root (contains src/Nexo.CLI).</param>
    /// <returns>Path to Nexo.CLI.dll.</returns>
    public static Task<string> EnsureCliBuiltAsync(string repoRoot)
    {
        var cliDll = Path.Combine(repoRoot, "src", "Nexo.CLI", "bin", "Debug", "net8.0", "Nexo.CLI.dll");

        lock (_cliBuildLock)
        {
            if (_cachedCliPath != null && File.Exists(_cachedCliPath))
                return Task.FromResult(_cachedCliPath);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build src/Nexo.CLI/Nexo.CLI.csproj --verbosity quiet",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet build");
            var stderrTask = p.StandardError.ReadToEndAsync();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                var err = stderrTask.GetAwaiter().GetResult();
                throw new InvalidOperationException($"CLI build failed (exit {p.ExitCode}): {err}");
            }

            if (!File.Exists(cliDll))
                throw new InvalidOperationException($"CLI DLL not found at {cliDll} after build");

            _cachedCliPath = cliDll;
            return Task.FromResult(cliDll);
        }
    }
}

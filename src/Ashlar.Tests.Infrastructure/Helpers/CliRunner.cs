using System.Diagnostics;
using System.Threading;

namespace Ashlar.Tests.Infrastructure.Helpers;

/// <summary>
/// Shared helper for running the Ashlar CLI in E2E tests.
/// Consolidates build-once and run logic from Phases14CliE2ETests and similar tests.
/// </summary>
public static class CliRunner
{
    private static readonly object _cliBuildLock = new();
    private static readonly Dictionary<string, string> _cachedCliPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Serializes <c>dotnet build</c> across parallel test hosts (e.g. net8.0 + net10.0) so
    /// concurrent MSBuild invocations do not corrupt shared <c>obj</c> trees.
    /// </summary>
    private static readonly Mutex s_crossProcessBuild =
        new(initiallyOwned: false, name: "Ashlar.CliRunner.EnsureCliBuilt.v1");

    /// <summary>
    /// Runs the prebuilt Ashlar CLI with the given arguments.
    /// </summary>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="args">CLI arguments (e.g. "adapt --dry-run").</param>
    /// <param name="envOverrides">Optional environment variable overrides (e.g. for test isolation).</param>
    /// <param name="timeout">Optional timeout. When provided, cancels and kills the process if it exceeds the limit.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>Exit code, stdout, and stderr.</returns>
    /// <param name="buildConfiguration">MSBuild configuration for the CLI project (e.g. Debug, Release). Cached separately per configuration.</param>
    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string workingDir,
        string args,
        IReadOnlyDictionary<string, string?>? envOverrides = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string buildConfiguration = "Debug")
    {
        using var timeoutCts = timeout.HasValue ? CancellationTokenSource.CreateLinkedTokenSource(ct) : null;
        if (timeout.HasValue && timeoutCts != null)
            timeoutCts.CancelAfter(timeout.Value);

        var effectiveCt = timeoutCts?.Token ?? ct;

        var cliPath = await EnsureCliBuiltAsync(workingDir, buildConfiguration);
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
    /// Set to <c>1</c>/<c>true</c> to rebuild the CLI from inside the test host even when a
    /// built <c>Ashlar.CLI.dll</c> already exists (local edit-run loops on the CLI itself).
    /// </summary>
    public const string ForceRebuildVariable = "ASHLAR_CLI_FORCE_REBUILD";

    /// <summary>
    /// Ensures the Ashlar CLI is built and returns the path to Ashlar.CLI.dll.
    ///
    /// <para>An already-built CLI wins. Every CI lane that runs these tests builds the CLI in a
    /// setup step before any test host exists (the readiness gate's "Setup — build CLI", the
    /// cross-platform matrix's <c>dotnet build Ashlar.sln</c>). Building it again from INSIDE a
    /// test host is what failed every RuntimeStudio smoke test on Windows: the CLI's project
    /// graph includes the test projects (its <c>test</c> command discovers them by assembly
    /// name), so the build also rebuilt <c>Ashlar.Tests.Infrastructure</c> — into the very
    /// <c>bin</c> the running testhost had locked. Only when no CLI has been built yet does
    /// this fall back to building one (once, serialized across hosts).</para>
    /// </summary>
    /// <param name="repoRoot">Repository root (contains application/src/Ashlar.CLI).</param>
    /// <param name="buildConfiguration">Debug (default) or Release for production-shaped binaries.</param>
    /// <returns>Path to Ashlar.CLI.dll.</returns>
    public static Task<string> EnsureCliBuiltAsync(string repoRoot, string buildConfiguration = "Debug")
    {
        if (string.IsNullOrWhiteSpace(buildConfiguration))
            buildConfiguration = "Debug";

        var cliDll = Path.Combine(repoRoot, "application", "src", "Ashlar.CLI", "bin", buildConfiguration, "net10.0", "Ashlar.CLI.dll");

        lock (_cliBuildLock)
        {
            if (_cachedCliPaths.TryGetValue(buildConfiguration, out var cached) && File.Exists(cached))
                return Task.FromResult(cached);

            if (File.Exists(cliDll) && !ForceRebuildRequested())
            {
                _cachedCliPaths[buildConfiguration] = cliDll;
                return Task.FromResult(cliDll);
            }
        }

        var acquired = false;
        try
        {
            acquired = s_crossProcessBuild.WaitOne(TimeSpan.FromMinutes(15));
            if (!acquired)
                throw new TimeoutException("Timed out waiting for Ashlar CLI build mutex (another test host is building).");

            lock (_cliBuildLock)
            {
                if (_cachedCliPaths.TryGetValue(buildConfiguration, out var hit) && File.Exists(hit))
                    return Task.FromResult(hit);

                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments =
                        $"build application/src/Ashlar.CLI/Ashlar.CLI.csproj -c {buildConfiguration} --verbosity quiet -p:TreatWarningsAsErrors=false",
                    WorkingDirectory = repoRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet build");
                var stderrTask = p.StandardError.ReadToEndAsync();
                var stdoutTask = p.StandardOutput.ReadToEndAsync();
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    var err = stderrTask.GetAwaiter().GetResult();
                    var outp = stdoutTask.GetAwaiter().GetResult();
                    throw new InvalidOperationException(
                        $"CLI build failed (exit {p.ExitCode}). stderr: {err}. stdout: {outp}");
                }

                if (!File.Exists(cliDll))
                    throw new InvalidOperationException($"CLI DLL not found at {cliDll} after build");

                _cachedCliPaths[buildConfiguration] = cliDll;
                return Task.FromResult(cliDll);
            }
        }
        finally
        {
            if (acquired)
                s_crossProcessBuild.ReleaseMutex();
        }
    }

    private static bool ForceRebuildRequested()
    {
        var value = Environment.GetEnvironmentVariable(ForceRebuildVariable);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}

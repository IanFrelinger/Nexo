using Ashlar.Tests.Infrastructure;

namespace Ashlar.Tests.Infrastructure.Helpers;

/// <summary>
/// Base class for CLI E2E tests. Provides repo root, temp dir, and process timeout for CliRunner.
/// </summary>
public abstract class E2ETestBase : TempDirTestBase
{
    /// <summary>
    /// Repository root (contains Ashlar.sln).
    /// </summary>
    protected string RepoRoot { get; }

    /// <summary>
    /// Default timeout for CLI invocations (55s, under blame-hang 60s).
    /// </summary>
    protected static TimeSpan DefaultCliTimeout => TimeSpan.FromSeconds(55);

    /// <summary>
    /// Creates an E2E test with temp dir and repo root.
    /// </summary>
    /// <param name="prefix">Prefix for the temp dir (e.g. "ashlar-phases59-e2e").</param>
    protected E2ETestBase(string prefix = "ashlar-e2e") : base(prefix)
    {
        RepoRoot = TestPaths.FindRepoRoot();
    }

    /// <summary>
    /// Runs the CLI with timeout to avoid orphaned processes and blame-hang.
    /// </summary>
    /// <param name="cliBuildConfiguration">Pass <c>Release</c> to exercise production-shaped CLI binaries.</param>
    protected Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(
        string args,
        IReadOnlyDictionary<string, string?>? envOverrides = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string cliBuildConfiguration = "Debug")
    {
        return CliRunner.RunAsync(RepoRoot, args, envOverrides, timeout ?? DefaultCliTimeout, ct, cliBuildConfiguration);
    }
}

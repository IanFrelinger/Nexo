using System.Diagnostics;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Shared Docker ArgumentList hardening: network isolation, resource caps, wall-clock timeout.
/// All args are passed via <see cref="ProcessStartInfo.ArgumentList"/> (no shell interpolation).
/// </summary>
public static class DockerHardening
{
    public const string BuildTimeoutEnv = "NEXO_DOCKER_BUILD_TIMEOUT_SEC";
    public const string RunTimeoutEnv = "NEXO_DOCKER_RUN_TIMEOUT_SEC";
    public const string MemoryEnv = "NEXO_DOCKER_MEMORY";
    public const string CpusEnv = "NEXO_DOCKER_CPUS";
    public const string PidsEnv = "NEXO_DOCKER_PIDS_LIMIT";

    public static int BuildTimeoutSeconds =>
        int.TryParse(Environment.GetEnvironmentVariable(BuildTimeoutEnv), out var s) && s > 0 ? s : 600;

    public static int RunTimeoutSeconds =>
        int.TryParse(Environment.GetEnvironmentVariable(RunTimeoutEnv), out var s) && s > 0 ? s : 120;

    public static string MemoryLimit =>
        Environment.GetEnvironmentVariable(MemoryEnv) is { Length: > 0 } m ? m : "2g";

    public static string CpusLimit =>
        Environment.GetEnvironmentVariable(CpusEnv) is { Length: > 0 } c ? c : "2";

    public static string PidsLimit =>
        Environment.GetEnvironmentVariable(PidsEnv) is { Length: > 0 } p ? p : "256";

    /// <summary>Append <c>docker build</c> isolation flags (BuildKit supports --network=none).</summary>
    public static void AddBuildIsolationArgs(ProcessStartInfo psi)
    {
        psi.ArgumentList.Add("--network=none");
        // BuildKit ignores run-style --memory on some hosts; still pass when supported.
        psi.ArgumentList.Add("--memory");
        psi.ArgumentList.Add(MemoryLimit);
    }

    /// <summary>Append <c>docker run</c> isolation flags (network none + cgroup caps).</summary>
    public static void AddRunIsolationArgs(ProcessStartInfo psi)
    {
        psi.ArgumentList.Add("--network=none");
        psi.ArgumentList.Add("--memory");
        psi.ArgumentList.Add(MemoryLimit);
        psi.ArgumentList.Add("--cpus");
        psi.ArgumentList.Add(CpusLimit);
        psi.ArgumentList.Add("--pids-limit");
        psi.ArgumentList.Add(PidsLimit);
    }

    public static async Task<int> WaitForExitWithTimeoutAsync(
        Process process,
        int timeoutSeconds,
        CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException(
                $"Docker process exceeded wall-clock timeout of {timeoutSeconds}s and was killed.");
        }
    }
}

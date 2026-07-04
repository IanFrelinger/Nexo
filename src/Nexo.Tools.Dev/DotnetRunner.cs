using System.Diagnostics;

namespace Nexo.Tools.Dev;

/// <summary>Dotnet runner.</summary>
internal static class DotnetRunner
{
    public static async Task<(int exitCode, string stdout, string stderr, bool timedOut)> RunAsync(
        string workingDirectory,
        string arguments,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var p = Process.Start(psi)!;
        var soTask = p.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var seTask = p.StandardError.ReadToEndAsync(timeoutCts.Token);

        try
        {
            await p.WaitForExitAsync(timeoutCts.Token);
            return (p.ExitCode, await soTask, await seTask, timedOut: false);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }

            var so = "";
            var se = "";
            try { so = await soTask; } catch { /* ignore */ }
            try { se = await seTask; } catch { /* ignore */ }
            return (-1, so, se, timedOut: true);
        }
    }
}


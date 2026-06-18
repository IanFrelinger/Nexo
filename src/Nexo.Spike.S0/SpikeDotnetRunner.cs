using System.Diagnostics;

namespace Nexo.Spike.S0;

internal static class SpikeDotnetRunner
{
    public const string TrxNoBuildArguments =
        "test -c Release --logger trx --no-build --blame-hang-timeout 60s --blame-hang-dump-type none --verbosity minimal";

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
            await p.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            return (p.ExitCode, await soTask.ConfigureAwait(false), await seTask.ConfigureAwait(false), false);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }

            var so = "";
            var se = "";
            try { so = await soTask.ConfigureAwait(false); } catch { /* ignore */ }
            try { se = await seTask.ConfigureAwait(false); } catch { /* ignore */ }
            return (-1, so, se, true);
        }
    }
}

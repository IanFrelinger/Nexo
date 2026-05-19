using System.Diagnostics;
using System.Text;

namespace Nexo.Tests.Infrastructure.Helpers;

/// <summary>Runs mesh-lab bash scripts and docker compose with captured output.</summary>
internal static class MeshLabProcessRunner
{
    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string workingDirectory,
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string?>? environment = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = timeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        if (timeout.HasValue && timeoutCts is not null)
            timeoutCts.CancelAfter(timeout.Value);

        var effectiveCt = timeoutCts?.Token ?? cancellationToken;

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                if (value is null)
                    psi.Environment.Remove(key);
                else
                    psi.Environment[key] = value;
            }
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {fileName}");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(effectiveCt);
        var stderrTask = process.StandardError.ReadToEndAsync(effectiveCt);

        try
        {
            await process.WaitForExitAsync(effectiveCt).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.HasValue)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignored
            }

            throw new TimeoutException(
                $"Process timed out after {timeout.Value.TotalSeconds:F0}s: {fileName} {arguments}");
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        return (process.ExitCode, stdout, stderr);
    }

    public static async Task AssertSuccessAsync(
        string workingDirectory,
        string fileName,
        string arguments,
        IReadOnlyDictionary<string, string?>? environment = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var (exitCode, stdout, stderr) = await RunAsync(
                workingDirectory,
                fileName,
                arguments,
                environment,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);

        if (exitCode == 0)
            return;

        var message = new StringBuilder();
        message.AppendLine($"{fileName} {arguments} exited {exitCode}");
        if (!string.IsNullOrWhiteSpace(stdout))
            message.AppendLine("stdout:").AppendLine(stdout);
        if (!string.IsNullOrWhiteSpace(stderr))
            message.AppendLine("stderr:").AppendLine(stderr);

        throw new InvalidOperationException(message.ToString());
    }
}

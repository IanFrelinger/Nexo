using System.Diagnostics;
using System.Text;

namespace Ashlar.Tests.Infrastructure.Helpers;

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
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
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

        var result = await Ashlar.Infrastructure.HostProcess.TimedProcess.RunAsync(
                psi,
                timeout ?? Timeout.InfiniteTimeSpan,
                cancellationToken)
            .ConfigureAwait(false);
        if (result.TimedOut)
        {
            throw new TimeoutException(
                $"Process timed out after {timeout!.Value.TotalSeconds:F0}s: {fileName} {arguments}");
        }

        return (result.ExitCode, result.StdOut, result.StdErr);
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

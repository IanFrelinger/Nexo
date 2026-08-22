using System.Diagnostics;
using System.Text;
using Ashlar.Core.Application.Execution.Ports;

namespace Ashlar.Infrastructure.Scaling;

/// <summary>Default <see cref="IProcessCommandRunner"/> using <see cref="Process"/>.</summary>
public sealed class ProcessCommandRunner : IProcessCommandRunner
{
    /// <inheritdoc />
    public async Task<ProcessCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };

        if (!process.Start())
            return new ProcessCommandResult(-1, "", $"Failed to start '{fileName}'.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return new ProcessCommandResult(process.ExitCode, stdout.ToString().Trim(), stderr.ToString().Trim());
    }
}

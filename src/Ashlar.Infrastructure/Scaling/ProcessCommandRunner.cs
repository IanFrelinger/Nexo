using System.Diagnostics;
using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Infrastructure.HostProcess;

namespace Ashlar.Infrastructure.Scaling;

/// <summary>Default <see cref="IProcessCommandRunner"/> using <see cref="System.Diagnostics.Process"/>.</summary>
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
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        // Infinite wait still kill-on-cancel: sandbox timeouts CancelAfter and must
        // not leave a docker child attached to a wedged daemon.
        var result = await TimedProcess.RunAsync(psi, Timeout.InfiniteTimeSpan, cancellationToken)
            .ConfigureAwait(false);
        return new ProcessCommandResult(result.ExitCode, result.StdOut, result.StdErr);
    }
}

using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Infrastructure.Scaling;

/// <summary>
/// Thin process runner used by kubectl/compose adapters so unit tests can stub shell I/O.
/// </summary>
public interface IProcessCommandRunner
{
    /// <summary>Runs a process and captures stdout/stderr/exit code.</summary>
    Task<ProcessCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}

namespace Ashlar.Core.Application.Execution.Ports;

/// <summary>
/// Runs a command under an isolation policy described by <see cref="SandboxSpec"/>.
/// Concrete backends (e.g. a container engine) live in infrastructure; profiles
/// only supply specs.
/// </summary>
public interface ISandboxedCommandRunner
{
    /// <summary>
    /// Executes <paramref name="spec"/> and returns stdout/stderr/exit code.
    /// </summary>
    Task<ProcessCommandResult> RunAsync(
        SandboxSpec spec,
        CancellationToken cancellationToken = default);
}

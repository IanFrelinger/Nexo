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

/// <summary>Result of a subprocess invocation.</summary>
public sealed record ProcessCommandResult(int ExitCode, string StdOut, string StdErr)
{
    /// <summary>True when <see cref="ExitCode"/> is zero.</summary>
    public bool Succeeded => ExitCode == 0;
}

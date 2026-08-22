namespace Ashlar.Core.Application.Execution.Ports;

/// <summary>Result of a subprocess or sandboxed command invocation.</summary>
public sealed record ProcessCommandResult(int ExitCode, string StdOut, string StdErr)
{
    /// <summary>True when <see cref="ExitCode"/> is zero.</summary>
    public bool Succeeded => ExitCode == 0;
}

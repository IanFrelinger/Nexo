namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Swappable sandbox for local skill script execution.
/// </summary>
public interface INexoScriptSandbox
{
    Task<SandboxScriptResult> RunAsync(
        string scriptAbsolutePath,
        string workingDirectory,
        TimeSpan timeout,
        int maxOutputBytes,
        CancellationToken cancellationToken = default);
}

/// <summary>Raw sandbox execution result.</summary>
public sealed record SandboxScriptResult(
    string StandardOutput,
    string StandardError,
    int ExitCode,
    bool OutputTruncated,
    TimeSpan Duration);

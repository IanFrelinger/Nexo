namespace Nexo.Core.Application.Skills.Models;

/// <summary>Outcome of a sandboxed skill script run.</summary>
public sealed record NexoScriptRunResult(
    string Output,
    int ExitCode,
    TimeSpan Duration,
    bool OutputTruncated,
    string ScriptContentHash,
    string Outcome);

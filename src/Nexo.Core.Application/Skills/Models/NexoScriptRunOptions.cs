namespace Nexo.Core.Application.Skills.Models;

/// <summary>Enforcement limits for local skill script execution.</summary>
public sealed record NexoScriptRunOptions(
    TimeSpan Timeout,
    int MaxOutputBytes,
    string WorkingDirectoryRoot);

namespace Nexo.Orchestration.Build.Models;

/// <summary>
/// Build warning information.
/// </summary>
public sealed record BuildWarning
{
    /// <summary>Warning code identifier.</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable warning message.</summary>
    public required string Message { get; init; }

    /// <summary>Source file associated with the warning.</summary>
    public string? File { get; init; }
}

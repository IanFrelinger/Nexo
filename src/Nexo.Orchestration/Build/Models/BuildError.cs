namespace Nexo.Orchestration.Build.Models;

/// <summary>
/// Build error information.
/// </summary>
public sealed record BuildError
{
    /// <summary>Error code identifier.</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable error message.</summary>
    public required string Message { get; init; }

    /// <summary>Source file associated with the error.</summary>
    public string? File { get; init; }

    /// <summary>Source line number, if available.</summary>
    public int? Line { get; init; }

    /// <summary>Severity classification of the error.</summary>
    public BuildErrorSeverity Severity { get; init; } = BuildErrorSeverity.Error;
}

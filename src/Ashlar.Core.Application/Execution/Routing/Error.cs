namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Lightweight error payload for Result/Error control-flow.
/// </summary>
public sealed record Error
{
    /// <summary>Machine-readable error code.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Human-readable error message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Optional additional diagnostic detail.</summary>
    public string? Detail { get; init; }
}

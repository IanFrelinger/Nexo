namespace Ashlar.Core.Application.Adaptation.Models;

/// <summary>
/// Audit entry for every adaptation attempt (success or failure).
/// </summary>
public record AdaptationAuditEntry
{
    /// <summary>Unique audit entry identifier.</summary>
    public required string Id { get; init; }

    /// <summary>UTC timestamp when the attempt occurred.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Autonomy level at time of attempt (supervised, semi-autonomous, fully-autonomous).</summary>
    public required string AutonomyLevel { get; init; }

    /// <summary>Outcome label (success, failure, rejected, etc.).</summary>
    public required string Outcome { get; init; }

    /// <summary>Brick identifier when the attempt targeted a brick.</summary>
    public string? BrickId { get; init; }

    /// <summary>Failure type that triggered the attempt.</summary>
    public string? FailureType { get; init; }

    /// <summary>Source file path when a source-level fix was attempted.</summary>
    public string? FilePath { get; init; }

    /// <summary>Whether regression tests passed.</summary>
    public bool RegressionPassed { get; init; }

    /// <summary>Whether the fix was promoted.</summary>
    public bool Promoted { get; init; }

    /// <summary>Human-readable outcome or error message.</summary>
    public string? Message { get; init; }
}

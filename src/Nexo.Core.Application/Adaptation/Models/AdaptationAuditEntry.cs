namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Audit entry for every adaptation attempt (success or failure).
/// </summary>
public record AdaptationAuditEntry
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string AutonomyLevel { get; init; }
    public required string Outcome { get; init; }
    public string? BrickId { get; init; }
    public string? FailureType { get; init; }
    public string? FilePath { get; init; }
    public bool RegressionPassed { get; init; }
    public bool Promoted { get; init; }
    public string? Message { get; init; }
}

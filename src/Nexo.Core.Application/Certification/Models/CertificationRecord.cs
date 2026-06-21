namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Signed certification admission record emitted on ADMIT.
/// </summary>
public sealed record CertificationRecord
{
    public required string Status { get; init; }
    public required string Stage { get; init; }
    public required bool Admitted { get; init; }
    public required bool Signed { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string BrickId { get; init; }
    public double? EscapeRate { get; init; }
    public int? TotalMutants { get; init; }
    public int? SurvivingMutants { get; init; }
    public IReadOnlyList<string> KilledMutants { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SurvivingMutantIds { get; init; } = Array.Empty<string>();
    public string? Signature { get; init; }
    public string? Reason { get; init; }
    public string? Gate { get; init; }
}

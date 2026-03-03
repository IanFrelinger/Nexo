namespace Nexo.Core.Application.Rollback.Models;

/// <summary>
/// Describes the impact of rolling back an adaptation.
/// </summary>
public record RollbackImpact(
    string TargetAdaptationId,
    IReadOnlyList<string> AdditionalComponentsAffected,
    bool WillCauseDataLoss,
    string Summary);

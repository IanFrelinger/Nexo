namespace Ashlar.Core.Application.Rollback.Models;

/// <summary>Describes the impact of rolling back an adaptation.</summary>
/// <param name="TargetAdaptationId">Adaptation identifier being rolled back.</param>
/// <param name="AdditionalComponentsAffected">Other components impacted by the rollback.</param>
/// <param name="WillCauseDataLoss">Whether rollback may discard persisted state.</param>
/// <param name="Summary">Human-readable impact summary.</param>
public record RollbackImpact(
    string TargetAdaptationId,
    IReadOnlyList<string> AdditionalComponentsAffected,
    bool WillCauseDataLoss,
    string Summary);

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Multi-cloud disaster recovery capabilities
/// </summary>
public partial interface IMultiCloudOrchestrator
{
    /// <summary>
    /// Implements disaster recovery across cloud providers
    /// </summary>
    /// <param name="disasterRecoveryRequest">Disaster recovery request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Disaster recovery result</returns>
    Task<MultiCloudDisasterRecoveryResult> ImplementDisasterRecoveryAsync(MultiCloudDisasterRecoveryRequest disasterRecoveryRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets disaster recovery status
    /// </summary>
    /// <param name="disasterRecoveryId">Disaster recovery identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Disaster recovery status</returns>
    Task<MultiCloudDisasterRecoveryStatus> GetDisasterRecoveryStatusAsync(string disasterRecoveryId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Multi-cloud disaster recovery request
/// </summary>
public record MultiCloudDisasterRecoveryRequest
{
    public string RecoveryPlanName { get; init; } = string.Empty;
    public string PrimaryProvider { get; init; } = string.Empty;
    public string SecondaryProvider { get; init; } = string.Empty;
    public DisasterRecoveryStrategy Strategy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public List<string> CriticalServices { get; init; } = new();
}

/// <summary>
/// Disaster recovery strategy
/// </summary>
public enum DisasterRecoveryStrategy
{
    BackupAndRestore,
    PilotLight,
    WarmStandby,
    MultiSite
}

/// <summary>
/// Multi-cloud disaster recovery result
/// </summary>
public record MultiCloudDisasterRecoveryResult
{
    public string RecoveryPlanId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string PrimaryProvider { get; init; } = string.Empty;
    public string SecondaryProvider { get; init; } = string.Empty;
    public DisasterRecoveryStrategy Strategy { get; init; }
    public List<RecoveryStep> Steps { get; init; } = new();
}

/// <summary>
/// Recovery step
/// </summary>
public record RecoveryStep
{
    public string StepName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Order { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Multi-cloud disaster recovery status
/// </summary>
public record MultiCloudDisasterRecoveryStatus
{
    public string RecoveryPlanId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CurrentProvider { get; init; } = string.Empty;
    public List<RecoveryStep> Steps { get; init; } = new();
    public DateTime LastTested { get; init; }
    public TimeSpan RecoveryTimeObjective { get; init; }
    public TimeSpan RecoveryPointObjective { get; init; }
}

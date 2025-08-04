using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Interface for cross-cloud deployment strategies
/// </summary>
public interface ICrossCloudDeploymentStrategy
{
    /// <summary>
    /// Gets the strategy name
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Gets the strategy description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the strategy type
    /// </summary>
    DeploymentStrategy StrategyType { get; }

    /// <summary>
    /// Executes the deployment strategy
    /// </summary>
    /// <param name="request">Deployment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deployment result</returns>
    Task<MultiCloudDeploymentResult> ExecuteAsync(MultiCloudDeploymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the strategy can be applied to the request
    /// </summary>
    /// <param name="request">Deployment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<StrategyValidationResult> ValidateAsync(MultiCloudDeploymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets strategy requirements
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Strategy requirements</returns>
    Task<StrategyRequirements> GetRequirementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates deployment time
    /// </summary>
    /// <param name="request">Deployment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Time estimation</returns>
    Task<TimeEstimation> EstimateDeploymentTimeAsync(MultiCloudDeploymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets strategy risks and mitigations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risks and mitigations</returns>
    Task<List<StrategyRisk>> GetRisksAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Strategy validation result
/// </summary>
public record StrategyValidationResult
{
    public bool IsValid { get; init; }
    public string StrategyName { get; init; } = string.Empty;
    public List<ValidationIssue> Issues { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object> Recommendations { get; init; } = new();
}

/// <summary>
/// Strategy requirements
/// </summary>
public record StrategyRequirements
{
    public string StrategyName { get; init; } = string.Empty;
    public int MinimumProviders { get; init; }
    public int MaximumProviders { get; init; }
    public List<string> RequiredServices { get; init; } = new();
    public List<string> RequiredCapabilities { get; init; } = new();
    public Dictionary<string, object> ConfigurationRequirements { get; init; } = new();
    public List<string> Prerequisites { get; init; } = new();
}

/// <summary>
/// Time estimation
/// </summary>
public record TimeEstimation
{
    public string StrategyName { get; init; } = string.Empty;
    public TimeSpan EstimatedDuration { get; init; }
    public TimeSpan MinimumDuration { get; init; }
    public TimeSpan MaximumDuration { get; init; }
    public List<TimeEstimationFactor> Factors { get; init; } = new();
    public double Confidence { get; init; }
}

/// <summary>
/// Time estimation factor
/// </summary>
public record TimeEstimationFactor
{
    public string Factor { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public TimeSpan AdditionalTime { get; init; }
    public double Probability { get; init; }
}

/// <summary>
/// Strategy risk
/// </summary>
public record StrategyRisk
{
    public string RiskName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public double Probability { get; init; }
    public string Impact { get; init; } = string.Empty;
    public List<string> Mitigations { get; init; } = new();
    public List<string> Contingencies { get; init; } = new();
}

/// <summary>
/// Blue-green deployment strategy
/// </summary>
public interface IBlueGreenDeploymentStrategy : ICrossCloudDeploymentStrategy
{
    /// <summary>
    /// Switches traffic from blue to green environment
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Switch result</returns>
    Task<TrafficSwitchResult> SwitchTrafficAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back to blue environment
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rollback result</returns>
    Task<RollbackResult> RollbackAsync(string deploymentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rolling deployment strategy
/// </summary>
public interface IRollingDeploymentStrategy : ICrossCloudDeploymentStrategy
{
    /// <summary>
    /// Gets the current rolling deployment status
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rolling deployment status</returns>
    Task<RollingDeploymentStatus> GetRollingStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the rolling deployment
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pause result</returns>
    Task<PauseResult> PauseRollingAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the rolling deployment
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resume result</returns>
    Task<ResumeResult> ResumeRollingAsync(string deploymentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Canary deployment strategy
/// </summary>
public interface ICanaryDeploymentStrategy : ICrossCloudDeploymentStrategy
{
    /// <summary>
    /// Gets canary deployment status
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Canary deployment status</returns>
    Task<CanaryDeploymentStatus> GetCanaryStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes canary to full deployment
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Promotion result</returns>
    Task<PromotionResult> PromoteCanaryAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts canary traffic percentage
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="percentage">Traffic percentage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Adjustment result</returns>
    Task<TrafficAdjustmentResult> AdjustCanaryTrafficAsync(string deploymentId, int percentage, CancellationToken cancellationToken = default);
}

/// <summary>
/// Failover deployment strategy
/// </summary>
public interface IFailoverDeploymentStrategy : ICrossCloudDeploymentStrategy
{
    /// <summary>
    /// Gets failover status
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Failover status</returns>
    Task<FailoverStatus> GetFailoverStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers failover to secondary provider
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Failover result</returns>
    Task<FailoverResult> TriggerFailoverAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests failover procedure
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Failover test result</returns>
    Task<FailoverTestResult> TestFailoverAsync(string deploymentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Traffic switch result
/// </summary>
public record TrafficSwitchResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime SwitchedAt { get; init; }
    public string FromEnvironment { get; init; } = string.Empty;
    public string ToEnvironment { get; init; } = string.Empty;
    public TimeSpan SwitchDuration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Rollback result
/// </summary>
public record RollbackResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime RolledBackAt { get; init; }
    public string FromEnvironment { get; init; } = string.Empty;
    public string ToEnvironment { get; init; } = string.Empty;
    public TimeSpan RollbackDuration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Rolling deployment status
/// </summary>
public record RollingDeploymentStatus
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int CurrentBatch { get; init; }
    public int TotalBatches { get; init; }
    public int CompletedInstances { get; init; }
    public int TotalInstances { get; init; }
    public bool IsPaused { get; init; }
    public DateTime LastUpdated { get; init; }
    public TimeSpan ElapsedTime { get; init; }
}

/// <summary>
/// Pause result
/// </summary>
public record PauseResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime PausedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Resume result
/// </summary>
public record ResumeResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ResumedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Canary deployment status
/// </summary>
public record CanaryDeploymentStatus
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int CanaryTrafficPercentage { get; init; }
    public int ProductionTrafficPercentage { get; init; }
    public Dictionary<string, object> CanaryMetrics { get; init; } = new();
    public Dictionary<string, object> ProductionMetrics { get; init; } = new();
    public bool IsHealthy { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Promotion result
/// </summary>
public record PromotionResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime PromotedAt { get; init; }
    public int FinalTrafficPercentage { get; init; }
    public TimeSpan PromotionDuration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Traffic adjustment result
/// </summary>
public record TrafficAdjustmentResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public int PreviousPercentage { get; init; }
    public int NewPercentage { get; init; }
    public DateTime AdjustedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Failover status
/// </summary>
public record FailoverStatus
{
    public string DeploymentId { get; init; } = string.Empty;
    public string CurrentProvider { get; init; } = string.Empty;
    public string BackupProvider { get; init; } = string.Empty;
    public bool IsFailoverActive { get; init; }
    public DateTime LastFailover { get; init; }
    public TimeSpan RecoveryTimeObjective { get; init; }
    public TimeSpan RecoveryPointObjective { get; init; }
    public bool IsHealthy { get; init; }
}

/// <summary>
/// Failover result
/// </summary
public record FailoverResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FromProvider { get; init; } = string.Empty;
    public string ToProvider { get; init; } = string.Empty;
    public DateTime FailedOverAt { get; init; }
    public TimeSpan FailoverDuration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Failover test result
/// </summary>
public record FailoverTestResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public TimeSpan TestDuration { get; init; }
    public TimeSpan RecoveryTime { get; init; }
    public DateTime TestedAt { get; init; }
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
} 
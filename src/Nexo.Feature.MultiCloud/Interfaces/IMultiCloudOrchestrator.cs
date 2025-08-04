using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Provides comprehensive multi-cloud orchestration capabilities for cross-cloud deployment and management
/// </summary>
public interface IMultiCloudOrchestrator
{
    /// <summary>
    /// Gets the list of available cloud providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available cloud providers</returns>
    Task<List<CloudProviderInfo>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to all configured cloud providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connectivity test results for all providers</returns>
    Task<MultiCloudConnectivityResult> TestAllProvidersConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deploys an application across multiple cloud providers
    /// </summary>
    /// <param name="deploymentRequest">Multi-cloud deployment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-cloud deployment result</returns>
    Task<MultiCloudDeploymentResult> DeployAcrossProvidersAsync(MultiCloudDeploymentRequest deploymentRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployment status across all cloud providers
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deployment status across all providers</returns>
    Task<MultiCloudDeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales an application across multiple cloud providers
    /// </summary>
    /// <param name="scalingRequest">Multi-cloud scaling request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-cloud scaling result</returns>
    Task<MultiCloudScalingResult> ScaleAcrossProvidersAsync(MultiCloudScalingRequest scalingRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cost analysis across all cloud providers
    /// </summary>
    /// <param name="startDate">Start date for cost analysis</param>
    /// <param name="endDate">End date for cost analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost analysis across all providers</returns>
    Task<MultiCloudCostAnalysis> GetCostAnalysisAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes costs across cloud providers
    /// </summary>
    /// <param name="optimizationRequest">Cost optimization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost optimization recommendations</returns>
    Task<MultiCloudCostOptimization> OptimizeCostsAsync(MultiCloudCostOptimizationRequest optimizationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors health across all cloud providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status across all providers</returns>
    Task<MultiCloudHealthStatus> MonitorHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates workloads between cloud providers
    /// </summary>
    /// <param name="migrationRequest">Workload migration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result</returns>
    Task<MultiCloudMigrationResult> MigrateWorkloadAsync(MultiCloudMigrationRequest migrationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets migration status
    /// </summary>
    /// <param name="migrationId">Migration identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration status</returns>
    Task<MultiCloudMigrationStatus> GetMigrationStatusAsync(string migrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load balances traffic across cloud providers
    /// </summary>
    /// <param name="loadBalancingRequest">Load balancing request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Load balancing result</returns>
    Task<MultiCloudLoadBalancingResult> LoadBalanceTrafficAsync(MultiCloudLoadBalancingRequest loadBalancingRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets load balancing configuration
    /// </summary>
    /// <param name="loadBalancerId">Load balancer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Load balancing configuration</returns>
    Task<MultiCloudLoadBalancingConfig> GetLoadBalancingConfigAsync(string loadBalancerId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Synchronizes data across cloud providers
    /// </summary>
    /// <param name="syncRequest">Data synchronization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data synchronization result</returns>
    Task<MultiCloudDataSyncResult> SyncDataAcrossProvidersAsync(MultiCloudDataSyncRequest syncRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets data synchronization status
    /// </summary>
    /// <param name="syncId">Synchronization identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data synchronization status</returns>
    Task<MultiCloudDataSyncStatus> GetDataSyncStatusAsync(string syncId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cloud provider information
/// </summary>
public record CloudProviderInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool IsConnected { get; init; }
    public List<string> SupportedServices { get; init; } = new();
    public Dictionary<string, object> Configuration { get; init; } = new();
}

/// <summary>
/// Multi-cloud connectivity test result
/// </summary>
public record MultiCloudConnectivityResult
{
    public DateTime TestedAt { get; init; }
    public List<ProviderConnectivityResult> ProviderResults { get; init; } = new();
    public bool AllProvidersConnected { get; init; }
    public TimeSpan TotalTestDuration { get; init; }
}

/// <summary>
/// Provider connectivity result
/// </summary>
public record ProviderConnectivityResult
{
    public string ProviderName { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public long LatencyMs { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public DateTime TestedAt { get; init; }
}

/// <summary>
/// Multi-cloud deployment request
/// </summary>
public record MultiCloudDeploymentRequest
{
    public string ApplicationName { get; init; } = string.Empty;
    public string ApplicationVersion { get; init; } = string.Empty;
    public List<string> TargetProviders { get; init; } = new();
    public DeploymentStrategy Strategy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public Dictionary<string, object> EnvironmentVariables { get; init; } = new();
    public List<string> Dependencies { get; init; } = new();
}

/// <summary>
/// Deployment strategy
/// </summary>
public enum DeploymentStrategy
{
    BlueGreen,
    Rolling,
    Canary,
    AllAtOnce,
    Failover
}

/// <summary>
/// Multi-cloud deployment result
/// </summary>
public record MultiCloudDeploymentResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DeployedAt { get; init; }
    public List<ProviderDeploymentResult> ProviderResults { get; init; } = new();
    public Dictionary<string, string> Endpoints { get; init; } = new();
    public TimeSpan TotalDeploymentTime { get; init; }
}

/// <summary>
/// Provider deployment result
/// </summary>
public record ProviderDeploymentResult
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Endpoint { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime DeployedAt { get; init; }
    public TimeSpan DeploymentTime { get; init; }
}

/// <summary>
/// Multi-cloud deployment status
/// </summary>
public record MultiCloudDeploymentStatus
{
    public string DeploymentId { get; init; } = string.Empty;
    public string OverallStatus { get; init; } = string.Empty;
    public List<ProviderDeploymentStatus> ProviderStatuses { get; init; } = new();
    public DateTime LastUpdated { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
}

/// <summary>
/// Provider deployment status
/// </summary>
public record ProviderDeploymentStatus
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int HealthyInstances { get; init; }
    public int TotalInstances { get; init; }
    public double CpuUsage { get; init; }
    public double MemoryUsage { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Multi-cloud scaling request
/// </summary>
public record MultiCloudScalingRequest
{
    public string DeploymentId { get; init; } = string.Empty;
    public Dictionary<string, int> ProviderScaling { get; init; } = new();
    public ScalingStrategy Strategy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
}

/// <summary>
/// Scaling strategy
/// </summary>
public enum ScalingStrategy
{
    Proportional,
    Absolute,
    Auto,
    Manual
}

/// <summary>
/// Multi-cloud scaling result
/// </summary>
public record MultiCloudScalingResult
{
    public string ScalingId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime ScaledAt { get; init; }
    public List<ProviderScalingResult> ProviderResults { get; init; } = new();
    public TimeSpan TotalScalingTime { get; init; }
}

/// <summary>
/// Provider scaling result
/// </summary>
public record ProviderScalingResult
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int PreviousInstances { get; init; }
    public int NewInstances { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ScaledAt { get; init; }
}

/// <summary>
/// Multi-cloud cost analysis
/// </summary>
public record MultiCloudCostAnalysis
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalCost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<ProviderCostBreakdown> ProviderCosts { get; init; } = new();
    public Dictionary<string, decimal> ServiceCosts { get; init; } = new();
    public List<CostOptimizationRecommendation> Recommendations { get; init; } = new();
}

/// <summary>
/// Provider cost breakdown
/// </summary>
public record ProviderCostBreakdown
{
    public string ProviderName { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public Dictionary<string, decimal> ServiceCosts { get; init; } = new();
    public List<CostTrend> Trends { get; init; } = new();
}

/// <summary>
/// Cost trend
/// </summary>
public record CostTrend
{
    public DateTime Date { get; init; }
    public decimal Cost { get; init; }
    public decimal Change { get; init; }
    public double PercentageChange { get; init; }
}

/// <summary>
/// Cost optimization recommendation
/// </summary>
public record CostOptimizationRecommendation
{
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public decimal PotentialSavings { get; init; }
    public string Impact { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
}

/// <summary>
/// Multi-cloud cost optimization request
/// </summary>
public record MultiCloudCostOptimizationRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<string> TargetProviders { get; init; } = new();
    public OptimizationStrategy Strategy { get; init; }
    public Dictionary<string, object> Constraints { get; init; } = new();
}

/// <summary>
/// Optimization strategy
/// </summary>
public enum OptimizationStrategy
{
    Aggressive,
    Conservative,
    Balanced,
    Custom
}

/// <summary>
/// Multi-cloud cost optimization
/// </summary
public record MultiCloudCostOptimization
{
    public string OptimizationId { get; init; } = string.Empty;
    public DateTime OptimizedAt { get; init; }
    public decimal CurrentCost { get; init; }
    public decimal OptimizedCost { get; init; }
    public decimal PotentialSavings { get; init; }
    public List<CostOptimizationRecommendation> Recommendations { get; init; } = new();
    public List<OptimizationAction> Actions { get; init; } = new();
}

/// <summary>
/// Optimization action
/// </summary>
public record OptimizationAction
{
    public string Action { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public decimal Savings { get; init; }
    public string Impact { get; init; } = string.Empty;
    public bool IsAutomatic { get; init; }
}

/// <summary>
/// Multi-cloud health status
/// </summary>
public record MultiCloudHealthStatus
{
    public DateTime CheckedAt { get; init; }
    public string OverallStatus { get; init; } = string.Empty;
    public List<ProviderHealthStatus> ProviderStatuses { get; init; } = new();
    public List<HealthAlert> Alerts { get; init; } = new();
}

/// <summary>
/// Provider health status
/// </summary>
public record ProviderHealthStatus
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Uptime { get; init; }
    public double ResponseTime { get; init; }
    public List<string> Issues { get; init; } = new();
    public DateTime LastChecked { get; init; }
}

/// <summary>
/// Health alert
/// </summary>
public record HealthAlert
{
    public string AlertId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime RaisedAt { get; init; }
    public bool IsResolved { get; init; }
}

/// <summary>
/// Multi-cloud migration request
/// </summary>
public record MultiCloudMigrationRequest
{
    public string SourceProvider { get; init; } = string.Empty;
    public string TargetProvider { get; init; } = string.Empty;
    public List<string> Resources { get; init; } = new();
    public MigrationStrategy Strategy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
}

/// <summary>
/// Migration strategy
/// </summary>
public enum MigrationStrategy
{
    LiftAndShift,
    Replatform,
    Refactor,
    Rebuild
}

/// <summary>
/// Multi-cloud migration result
/// </summary>
public record MultiCloudMigrationResult
{
    public string MigrationId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public string SourceProvider { get; init; } = string.Empty;
    public string TargetProvider { get; init; } = string.Empty;
    public List<MigrationStep> Steps { get; init; } = new();
    public TimeSpan EstimatedDuration { get; init; }
}

/// <summary>
/// Migration step
/// </summary>
public record MigrationStep
{
    public string StepName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Progress { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Multi-cloud migration status
/// </summary>
public record MultiCloudMigrationStatus
{
    public string MigrationId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int OverallProgress { get; init; }
    public List<MigrationStep> Steps { get; init; } = new();
    public DateTime LastUpdated { get; init; }
    public TimeSpan ElapsedTime { get; init; }
    public TimeSpan? EstimatedRemainingTime { get; init; }
}

/// <summary>
/// Multi-cloud load balancing request
/// </summary>
public record MultiCloudLoadBalancingRequest
{
    public string LoadBalancerName { get; init; } = string.Empty;
    public List<string> Providers { get; init; } = new();
    public LoadBalancingAlgorithm Algorithm { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public List<string> HealthChecks { get; init; } = new();
}

/// <summary>
/// Load balancing algorithm
/// </summary>
public enum LoadBalancingAlgorithm
{
    RoundRobin,
    LeastConnections,
    WeightedRoundRobin,
    LeastResponseTime,
    IPHash
}

/// <summary>
/// Multi-cloud load balancing result
/// </summary>
public record MultiCloudLoadBalancingResult
{
    public string LoadBalancerId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public List<ProviderLoadBalancerConfig> ProviderConfigs { get; init; } = new();
}

/// <summary>
/// Provider load balancer configuration
/// </summary>
public record ProviderLoadBalancerConfig
{
    public string ProviderName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public int Weight { get; init; }
    public bool IsHealthy { get; init; }
    public double ResponseTime { get; init; }
}

/// <summary>
/// Multi-cloud load balancing configuration
/// </summary>
public record MultiCloudLoadBalancingConfig
{
    public string LoadBalancerId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public LoadBalancingAlgorithm Algorithm { get; init; }
    public List<ProviderLoadBalancerConfig> ProviderConfigs { get; init; } = new();
    public Dictionary<string, object> Configuration { get; init; } = new();
    public DateTime LastUpdated { get; init; }
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

/// <summary>
/// Multi-cloud data synchronization request
/// </summary>
public record MultiCloudDataSyncRequest
{
    public string SyncName { get; init; } = string.Empty;
    public List<string> Providers { get; init; } = new();
    public List<string> DataSources { get; init; } = new();
    public SyncStrategy Strategy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public SyncSchedule Schedule { get; init; } = new();
}

/// <summary>
/// Sync strategy
/// </summary>
public enum SyncStrategy
{
    RealTime,
    NearRealTime,
    Batch,
    OnDemand
}

/// <summary>
/// Sync schedule
/// </summary>
public record SyncSchedule
{
    public string CronExpression { get; init; } = string.Empty;
    public TimeSpan Interval { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime? NextRun { get; init; }
}

/// <summary>
/// Multi-cloud data synchronization result
/// </summary>
public record MultiCloudDataSyncResult
{
    public string SyncId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public List<string> Providers { get; init; } = new();
    public List<DataSyncStep> Steps { get; init; } = new();
    public long TotalDataSize { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
}

/// <summary>
/// Data synchronization step
/// </summary>
public record DataSyncStep
{
    public string StepName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Progress { get; init; }
    public long DataSize { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Multi-cloud data synchronization status
/// </summary>
public record MultiCloudDataSyncStatus
{
    public string SyncId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int OverallProgress { get; init; }
    public List<DataSyncStep> Steps { get; init; } = new();
    public DateTime LastSync { get; init; }
    public DateTime? NextSync { get; init; }
    public long TotalDataSynced { get; init; }
    public TimeSpan ElapsedTime { get; init; }
} 
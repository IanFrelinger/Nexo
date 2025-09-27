using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Multi-cloud migration capabilities
/// </summary>
public partial interface IMultiCloudOrchestrator
{
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

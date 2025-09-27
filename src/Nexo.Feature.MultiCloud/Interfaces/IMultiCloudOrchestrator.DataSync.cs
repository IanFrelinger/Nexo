using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Multi-cloud data synchronization capabilities
/// </summary>
public partial interface IMultiCloudOrchestrator
{
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

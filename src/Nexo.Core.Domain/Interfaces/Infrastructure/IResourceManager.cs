using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for resource management
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Allocate resources for an operation
    /// </summary>
    Task<IResourceAllocation> AllocateResourcesAsync(ResourceRequirements requirements);
    
    /// <summary>
    /// Release allocated resources
    /// </summary>
    Task ReleaseResourcesAsync(string allocationId);
    
    /// <summary>
    /// Get current resource usage
    /// </summary>
    Task<ResourceUsage> GetCurrentResourceUsageAsync();
    
    /// <summary>
    /// Check if resources are available
    /// </summary>
    Task<bool> AreResourcesAvailableAsync(ResourceRequirements requirements);
    
    /// <summary>
    /// Get resource limits
    /// </summary>
    Task<ResourceLimits> GetResourceLimitsAsync();
    
    /// <summary>
    /// Set resource limits
    /// </summary>
    Task SetResourceLimitsAsync(ResourceLimits limits);
    
    /// <summary>
    /// Monitor resource usage
    /// </summary>
    Task StartResourceMonitoringAsync();
    
    /// <summary>
    /// Stop resource monitoring
    /// </summary>
    Task StopResourceMonitoringAsync();
}

/// <summary>
/// Resource requirements
/// </summary>
public record ResourceRequirements
{
    /// <summary>
    /// CPU cores required
    /// </summary>
    public int CpuCores { get; init; }
    
    /// <summary>
    /// Memory required in MB
    /// </summary>
    public long MemoryMB { get; init; }
    
    /// <summary>
    /// Disk space required in MB
    /// </summary>
    public long DiskSpaceMB { get; init; }
    
    /// <summary>
    /// Network bandwidth required in Mbps
    /// </summary>
    public double NetworkBandwidthMbps { get; init; }
    
    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public int MaxExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Priority level (1-10)
    /// </summary>
    public int Priority { get; init; } = 5;
}

/// <summary>
/// Resource allocation
/// </summary>
public record ResourceAllocation
{
    /// <summary>
    /// Allocation identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Allocated resources
    /// </summary>
    public ResourceRequirements AllocatedResources { get; init; } = new();
    
    /// <summary>
    /// Allocation timestamp
    /// </summary>
    public DateTime AllocatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Expiration time
    /// </summary>
    public DateTime ExpiresAt { get; init; }
    
    /// <summary>
    /// Whether allocation is active
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Resource usage information
/// </summary>
public record ResourceUsage
{
    /// <summary>
    /// Current CPU usage percentage
    /// </summary>
    public double CpuUsagePercentage { get; init; }
    
    /// <summary>
    /// Current memory usage in MB
    /// </summary>
    public long MemoryUsageMB { get; init; }
    
    /// <summary>
    /// Available memory in MB
    /// </summary>
    public long AvailableMemoryMB { get; init; }
    
    /// <summary>
    /// Current disk usage in MB
    /// </summary>
    public long DiskUsageMB { get; init; }
    
    /// <summary>
    /// Available disk space in MB
    /// </summary>
    public long AvailableDiskSpaceMB { get; init; }
    
    /// <summary>
    /// Current network usage in Mbps
    /// </summary>
    public double NetworkUsageMbps { get; init; }
    
    /// <summary>
    /// Number of active allocations
    /// </summary>
    public int ActiveAllocations { get; init; }
    
    /// <summary>
    /// Timestamp when usage was measured
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Resource limits
/// </summary>
public record ResourceLimits
{
    /// <summary>
    /// Maximum CPU cores
    /// </summary>
    public int MaxCpuCores { get; init; }
    
    /// <summary>
    /// Maximum memory in MB
    /// </summary>
    public long MaxMemoryMB { get; init; }
    
    /// <summary>
    /// Maximum disk space in MB
    /// </summary>
    public long MaxDiskSpaceMB { get; init; }
    
    /// <summary>
    /// Maximum network bandwidth in Mbps
    /// </summary>
    public double MaxNetworkBandwidthMbps { get; init; }
    
    /// <summary>
    /// Maximum concurrent allocations
    /// </summary>
    public int MaxConcurrentAllocations { get; init; }
    
    /// <summary>
    /// Default allocation timeout in milliseconds
    /// </summary>
    public int DefaultAllocationTimeoutMs { get; init; } = 300000; // 5 minutes
}
using System;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for resource allocation
/// </summary>
public interface IResourceAllocation
{
    /// <summary>
    /// Allocation identifier
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Allocated resources
    /// </summary>
    ResourceRequirements AllocatedResources { get; }
    
    /// <summary>
    /// Allocation timestamp
    /// </summary>
    DateTime AllocatedAt { get; }
    
    /// <summary>
    /// Expiration time
    /// </summary>
    DateTime ExpiresAt { get; }
    
    /// <summary>
    /// Whether allocation is active
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Release the allocated resources
    /// </summary>
    Task ReleaseAsync();
    
    /// <summary>
    /// Extend the allocation
    /// </summary>
    Task ExtendAsync(TimeSpan extension);
    
    /// <summary>
    /// Get current usage of allocated resources
    /// </summary>
    Task<ResourceUsage> GetCurrentUsageAsync();
}

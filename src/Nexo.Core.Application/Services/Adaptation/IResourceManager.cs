using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Interface for managing system resources
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Get current resource utilization
    /// </summary>
    Task<ResourceUtilization> GetCurrentUtilizationAsync();
    
    /// <summary>
    /// Get resource allocation
    /// </summary>
    Task<Nexo.Core.Domain.Entities.Infrastructure.ResourceAllocation> GetAllocationAsync();
    
    /// <summary>
    /// Set resource allocation
    /// </summary>
    Task SetAllocationAsync(Nexo.Core.Domain.Entities.Infrastructure.ResourceAllocation allocation);
    
    /// <summary>
    /// Get resource constraints
    /// </summary>
    Task<Nexo.Core.Domain.Entities.Infrastructure.ResourceConstraints> GetConstraintsAsync();
    
    /// <summary>
    /// Set resource constraints
    /// </summary>
    Task SetConstraintsAsync(Nexo.Core.Domain.Entities.Infrastructure.ResourceConstraints constraints);
    
    /// <summary>
    /// Check if resources are available
    /// </summary>
    Task<bool> AreResourcesAvailableAsync(ResourceUtilization requirements);
    
    /// <summary>
    /// Reserve resources
    /// </summary>
    Task<bool> ReserveResourcesAsync(ResourceUtilization requirements);
    
    /// <summary>
    /// Release resources
    /// </summary>
    Task ReleaseResourcesAsync(ResourceUtilization requirements);
    
    /// <summary>
    /// Get resource recommendations
    /// </summary>
    Task<IEnumerable<string>> GetResourceRecommendationsAsync();
    
    /// <summary>
    /// Optimize resource usage
    /// </summary>
    Task<OptimizationResult> OptimizeResourceUsageAsync();
}

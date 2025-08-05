using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Azure.Models;

namespace Nexo.Feature.Azure.Interfaces;

/// <summary>
/// Provides comprehensive Azure services integration capabilities
/// </summary>
public interface IAzureProvider
{
    /// <summary>
    /// Tests Azure connectivity and validates credentials
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating connectivity status</returns>
    Task<AzureConnectivityResult> TestConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves Azure subscription information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing subscription details</returns>
    Task<AzureSubscriptionInfo> GetSubscriptionInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Azure resource group information
    /// </summary>
    /// <param name="resourceGroupName">Name of the resource group</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing resource group details</returns>
    Task<AzureResourceGroupInfo> GetResourceGroupInfoAsync(string resourceGroupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all resource groups in the subscription
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of resource groups</returns>
    Task<List<AzureResourceGroupInfo>> ListResourceGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new resource group
    /// </summary>
    /// <param name="resourceGroupName">Name of the resource group</param>
    /// <param name="location">Azure region location</param>
    /// <param name="tags">Optional tags for the resource group</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating creation success</returns>
    Task<AzureOperationResult> CreateResourceGroupAsync(string resourceGroupName, string location, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resource group and all its resources
    /// </summary>
    /// <param name="resourceGroupName">Name of the resource group</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating deletion success</returns>
    Task<AzureOperationResult> DeleteResourceGroupAsync(string resourceGroupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Azure service health information
    /// </summary>
    /// <param name="region">Azure region (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing service health status</returns>
    Task<AzureServiceHealthInfo> GetServiceHealthAsync(string? region = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Azure cost and usage information
    /// </summary>
    /// <param name="startDate">Start date for cost analysis</param>
    /// <param name="endDate">End date for cost analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing cost and usage data</returns>
    Task<AzureCostInfo> GetCostAndUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available Azure regions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of available regions</returns>
    Task<List<AzureRegionInfo>> GetAvailableRegionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Azure resource naming conventions
    /// </summary>
    /// <param name="resourceName">Name to validate</param>
    /// <param name="resourceType">Type of Azure resource</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating validation status</returns>
    Task<AzureValidationResult> ValidateResourceNameAsync(string resourceName, string resourceType, CancellationToken cancellationToken = default);
} 
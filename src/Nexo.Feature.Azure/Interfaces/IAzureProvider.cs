using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

/// <summary>
/// Azure subscription information
/// </summary>
public record AzureSubscriptionInfo
{
    public string SubscriptionId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string LocationPlacementId { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Azure resource group information
/// </summary>
public record AzureResourceGroupInfo
{
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string ProvisioningState { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public int ResourceCount { get; init; }
}

/// <summary>
/// Azure service health information
/// </summary>
public record AzureServiceHealthInfo
{
    public string Region { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<AzureServiceIssue> Issues { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Azure service issue information
/// </summary>
public record AzureServiceIssue
{
    public string ServiceName { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
}

/// <summary>
/// Azure cost and usage information
/// </summary>
public record AzureCostInfo
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalCost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<AzureCostBreakdown> Breakdown { get; init; } = new();
    public Dictionary<string, decimal> ServiceCosts { get; init; } = new();
}

/// <summary>
/// Azure cost breakdown by service
/// </summary>
public record AzureCostBreakdown
{
    public string ServiceName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string ResourceName { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime UsageDate { get; init; }
}

/// <summary>
/// Azure region information
/// </summary>
public record AzureRegionInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string RegionalDisplayName { get; init; } = string.Empty;
    public List<string> AvailableServices { get; init; } = new();
    public bool IsAvailable { get; init; }
}

/// <summary>
/// Azure connectivity test result
/// </summary>
public class AzureConnectivityResult
{
    /// <summary>
    /// Whether the connection was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Connection message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Connection latency in milliseconds
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Test timestamp
    /// </summary>
    public DateTime TestedAt { get; set; }

    /// <summary>
    /// Error details if connection failed
    /// </summary>
    public string? ErrorDetails { get; set; }
}

/// <summary>
/// Azure operation result
/// </summary>
public class AzureOperationResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Operation message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Operation timestamp
    /// </summary>
    public DateTime OperatedAt { get; set; }

    /// <summary>
    /// Error details if operation failed
    /// </summary>
    public string? ErrorDetails { get; set; }
}

/// <summary>
/// Azure validation result
/// </summary>
public class AzureValidationResult
{
    /// <summary>
    /// Whether the validation was successful
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Validation timestamp
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    /// <summary>
    /// Error details if validation failed
    /// </summary>
    public string? ErrorDetails { get; set; }
} 
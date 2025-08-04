using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Factory for creating and managing cloud provider instances
/// </summary>
public interface ICloudProviderFactory
{
    /// <summary>
    /// Gets a cloud provider instance by name
    /// </summary>
    /// <param name="providerName">Name of the cloud provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cloud provider instance</returns>
    Task<ICloudProvider> GetProviderAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available cloud provider instances
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all cloud provider instances</returns>
    Task<List<ICloudProvider>> GetAllProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new cloud provider
    /// </summary>
    /// <param name="provider">Cloud provider to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    Task<ProviderRegistrationResult> RegisterProviderAsync(ICloudProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a cloud provider
    /// </summary>
    /// <param name="providerName">Name of the provider to unregister</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unregistration result</returns>
    Task<ProviderRegistrationResult> UnregisterProviderAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates cloud provider configuration
    /// </summary>
    /// <param name="providerName">Name of the provider to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ProviderValidationResult> ValidateProviderAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets provider capabilities
    /// </summary>
    /// <param name="providerName">Name of the provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider capabilities</returns>
    Task<ProviderCapabilities> GetProviderCapabilitiesAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares provider capabilities
    /// </summary>
    /// <param name="providerNames">Names of providers to compare</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider comparison result</returns>
    Task<ProviderComparisonResult> CompareProvidersAsync(List<string> providerNames, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cloud provider interface
/// </summary>
public interface ICloudProvider
{
    /// <summary>
    /// Gets the provider name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the provider display name
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the provider version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Tests connectivity to the provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connectivity test result</returns>
    Task<ProviderConnectivityResult> TestConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets provider information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider information</returns>
    Task<CloudProviderInfo> GetProviderInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets provider capabilities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider capabilities</returns>
    Task<ProviderCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets provider health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider health status</returns>
    Task<ProviderHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets provider cost information
    /// </summary>
    /// <param name="startDate">Start date for cost analysis</param>
    /// <param name="endDate">End date for cost analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider cost information</returns>
    Task<ProviderCostInfo> GetCostInfoAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider registration result
/// </summary>
public record ProviderRegistrationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Provider validation result
/// </summary>
public record ProviderValidationResult
{
    public bool IsValid { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public List<ValidationIssue> Issues { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Validation issue
/// </summary>
public record ValidationIssue
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string? SuggestedFix { get; init; }
}

/// <summary>
/// Provider capabilities
/// </summary>
public record ProviderCapabilities
{
    public string ProviderName { get; init; } = string.Empty;
    public List<string> SupportedServices { get; init; } = new();
    public List<string> SupportedRegions { get; init; } = new();
    public List<string> SupportedInstanceTypes { get; init; } = new();
    public Dictionary<string, object> ServiceLimits { get; init; } = new();
    public Dictionary<string, object> Pricing { get; init; } = new();
    public List<string> SecurityFeatures { get; init; } = new();
    public List<string> ComplianceStandards { get; init; } = new();
}

/// <summary>
/// Provider comparison result
/// </summary>
public record ProviderComparisonResult
{
    public List<string> ProviderNames { get; init; } = new();
    public Dictionary<string, ProviderCapabilities> Capabilities { get; init; } = new();
    public Dictionary<string, decimal> CostComparison { get; init; } = new();
    public Dictionary<string, double> PerformanceComparison { get; init; } = new();
    public List<ComparisonRecommendation> Recommendations { get; init; } = new();
    public DateTime ComparedAt { get; init; }
}

/// <summary>
/// Comparison recommendation
/// </summary>
public record ComparisonRecommendation
{
    public string UseCase { get; init; } = string.Empty;
    public string RecommendedProvider { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
}

/// <summary>
/// Provider cost information
/// </summary>
public record ProviderCostInfo
{
    public string ProviderName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalCost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public Dictionary<string, decimal> ServiceCosts { get; init; } = new();
    public Dictionary<string, decimal> RegionCosts { get; init; } = new();
    public List<CostTrend> Trends { get; init; } = new();
    public List<CostOptimizationRecommendation> Recommendations { get; init; } = new();
} 
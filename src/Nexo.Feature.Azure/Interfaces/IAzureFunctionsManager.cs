using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Azure.Interfaces;

/// <summary>
/// Provides Azure Functions deployment and management capabilities
/// </summary>
public interface IAzureFunctionsManager
{
    /// <summary>
    /// Deploys a function app to Azure Functions
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="sourcePath">Path to the source code</param>
    /// <param name="runtime">Runtime (dotnet, node, python, etc.)</param>
    /// <param name="region">Azure region</param>
    /// <param name="planType">App service plan type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing deployment information</returns>
    Task<FunctionAppDeploymentInfo> DeployFunctionAppAsync(string resourceGroupName, string functionAppName, string sourcePath, string runtime, string region, AppServicePlanType planType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing function app
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="sourcePath">Path to the updated source code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing update information</returns>
    Task<FunctionAppUpdateInfo> UpdateFunctionAppAsync(string resourceGroupName, string functionAppName, string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes an Azure Function
    /// </summary>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="functionName">Name of the function</param>
    /// <param name="payload">Function payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing function response</returns>
    Task<FunctionInvocationResult> InvokeFunctionAsync(string functionAppName, string functionName, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets function app information
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing function app details</returns>
    Task<FunctionAppInfo> GetFunctionAppInfoAsync(string resourceGroupName, string functionAppName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all function apps in a resource group
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of function apps</returns>
    Task<List<FunctionAppInfo>> ListFunctionAppsAsync(string resourceGroupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets function information within a function app
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="functionName">Name of the function</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing function details</returns>
    Task<FunctionInfo> GetFunctionInfoAsync(string resourceGroupName, string functionAppName, string functionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all functions in a function app
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of functions</returns>
    Task<List<FunctionInfo>> ListFunctionsAsync(string resourceGroupName, string functionAppName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets function app logs
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="startTime">Start time for log retrieval</param>
    /// <param name="endTime">End time for log retrieval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing log entries</returns>
    Task<List<FunctionLogEntry>> GetFunctionLogsAsync(string resourceGroupName, string functionAppName, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a deployment package from source code
    /// </summary>
    /// <param name="sourcePath">Path to the source code</param>
    /// <param name="outputPath">Path for the deployment package</param>
    /// <param name="runtime">Runtime version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing package information</returns>
    Task<DeploymentPackageInfo> CreateDeploymentPackageAsync(string sourcePath, string outputPath, string runtime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a function app
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating deletion success</returns>
    Task<AzureOperationResult> DeleteFunctionAppAsync(string resourceGroupName, string functionAppName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a function app
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating start success</returns>
    Task<AzureOperationResult> StartFunctionAppAsync(string resourceGroupName, string functionAppName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a function app
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating stop success</returns>
    Task<AzureOperationResult> StopFunctionAppAsync(string resourceGroupName, string functionAppName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets function app configuration
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing configuration settings</returns>
    Task<FunctionAppConfiguration> GetFunctionAppConfigurationAsync(string resourceGroupName, string functionAppName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates function app configuration
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="functionAppName">Name of the function app</param>
    /// <param name="configuration">Configuration settings to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating update success</returns>
    Task<AzureOperationResult> UpdateFunctionAppConfigurationAsync(string resourceGroupName, string functionAppName, FunctionAppConfiguration configuration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Function app deployment information
/// </summary>
public record FunctionAppDeploymentInfo
{
    public string ResourceGroupName { get; init; } = string.Empty;
    public string FunctionAppName { get; init; } = string.Empty;
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DeployedAt { get; init; }
    public string Runtime { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public AppServicePlanType PlanType { get; init; }
    public string Url { get; init; } = string.Empty;
}

/// <summary>
/// Function app update information
/// </summary>
public record FunctionAppUpdateInfo
{
    public string ResourceGroupName { get; init; } = string.Empty;
    public string FunctionAppName { get; init; } = string.Empty;
    public string UpdateId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public long PackageSize { get; init; }
}

/// <summary>
/// Function invocation result
/// </summary>
public record FunctionInvocationResult
{
    public string FunctionName { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public object? Response { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public TimeSpan ExecutionTime { get; init; }
    public DateTime InvokedAt { get; init; }
}

/// <summary>
/// Function app information
/// </summary>
public record FunctionAppInfo
{
    public string Name { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Runtime { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public AppServicePlanType PlanType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public int FunctionCount { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
}

/// <summary>
/// Function information
/// </summary>
public record FunctionInfo
{
    public string Name { get; init; } = string.Empty;
    public string FunctionAppName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string TriggerType { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public Dictionary<string, string> Configuration { get; init; } = new();
}

/// <summary>
/// Function log entry
/// </summary>
public record FunctionLogEntry
{
    public string FunctionName { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string InvocationId { get; init; } = string.Empty;
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Deployment package information
/// </summary>
public record DeploymentPackageInfo
{
    public string PackagePath { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Runtime { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Checksum { get; init; } = string.Empty;
}

/// <summary>
/// Function app configuration
/// </summary>
public record FunctionAppConfiguration
{
    public Dictionary<string, string> AppSettings { get; init; } = new();
    public Dictionary<string, string> ConnectionStrings { get; init; } = new();
    public Dictionary<string, string> SiteConfig { get; init; } = new();
    public List<string> EnabledFunctions { get; init; } = new();
    public Dictionary<string, object> FunctionConfigurations { get; init; } = new();
}

/// <summary>
/// App service plan types
/// </summary>
public enum AppServicePlanType
{
    Free,
    Basic,
    Standard,
    Premium,
    Isolated
} 
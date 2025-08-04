using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Azure.Interfaces;

/// <summary>
/// Provides Azure Kubernetes Service (AKS) container orchestration capabilities
/// </summary>
public interface IAKSContainerOrchestrator
{
    /// <summary>
    /// Creates an AKS cluster
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="region">Azure region</param>
    /// <param name="nodeCount">Number of nodes in the default node pool</param>
    /// <param name="vmSize">VM size for the nodes</param>
    /// <param name="kubernetesVersion">Kubernetes version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing cluster creation information</returns>
    Task<AKSClusterCreationInfo> CreateClusterAsync(string resourceGroupName, string clusterName, string region, int nodeCount, string vmSize, string kubernetesVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deploys a service to AKS
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="imageName">Container image name</param>
    /// <param name="replicas">Number of replicas</param>
    /// <param name="port">Service port</param>
    /// <param name="targetPort">Container target port</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing deployment information</returns>
    Task<AKSServiceDeploymentInfo> DeployServiceAsync(string clusterName, string resourceGroupName, string serviceName, string imageName, int replicas, int port, int targetPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales a service in AKS
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="replicas">New number of replicas</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating scaling success</returns>
    Task<AzureOperationResult> ScaleServiceAsync(string clusterName, string resourceGroupName, string serviceName, int replicas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AKS cluster information
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing cluster details</returns>
    Task<AKSClusterInfo> GetClusterInfoAsync(string resourceGroupName, string clusterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all AKS clusters in a resource group
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of clusters</returns>
    Task<List<AKSClusterInfo>> ListClustersAsync(string resourceGroupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets service information in AKS
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing service details</returns>
    Task<AKSServiceInfo> GetServiceInfoAsync(string clusterName, string resourceGroupName, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all services in an AKS cluster
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of services</returns>
    Task<List<AKSServiceInfo>> ListServicesAsync(string clusterName, string resourceGroupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pod information in AKS
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="podName">Name of the pod</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing pod details</returns>
    Task<AKSPodInfo> GetPodInfoAsync(string clusterName, string resourceGroupName, string podName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all pods in an AKS cluster
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of pods</returns>
    Task<List<AKSPodInfo>> ListPodsAsync(string clusterName, string resourceGroupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pod logs from AKS
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="podName">Name of the pod</param>
    /// <param name="containerName">Name of the container (optional)</param>
    /// <param name="tailLines">Number of lines to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing pod logs</returns>
    Task<List<AKSPodLogEntry>> GetPodLogsAsync(string clusterName, string resourceGroupName, string podName, string? containerName = null, int? tailLines = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command in a pod
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="podName">Name of the pod</param>
    /// <param name="containerName">Name of the container</param>
    /// <param name="command">Command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing command output</returns>
    Task<AKSCommandResult> ExecuteCommandAsync(string clusterName, string resourceGroupName, string podName, string containerName, string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a service from AKS
    /// </summary>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="serviceName">Name of the service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating deletion success</returns>
    Task<AzureOperationResult> DeleteServiceAsync(string clusterName, string resourceGroupName, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an AKS cluster
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating deletion success</returns>
    Task<AzureOperationResult> DeleteClusterAsync(string resourceGroupName, string clusterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cluster credentials for kubectl access
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing cluster credentials</returns>
    Task<AKSClusterCredentials> GetClusterCredentialsAsync(string resourceGroupName, string clusterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates cluster configuration
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="configuration">Cluster configuration to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating update success</returns>
    Task<AzureOperationResult> UpdateClusterConfigurationAsync(string resourceGroupName, string clusterName, AKSClusterConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets node pool information
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="nodePoolName">Name of the node pool</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing node pool details</returns>
    Task<AKSNodePoolInfo> GetNodePoolInfoAsync(string resourceGroupName, string clusterName, string nodePoolName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all node pools in an AKS cluster
    /// </summary>
    /// <param name="resourceGroupName">Resource group name</param>
    /// <param name="clusterName">Name of the AKS cluster</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of node pools</returns>
    Task<List<AKSNodePoolInfo>> ListNodePoolsAsync(string resourceGroupName, string clusterName, CancellationToken cancellationToken = default);
}

/// <summary>
/// AKS cluster creation information
/// </summary>
public record AKSClusterCreationInfo
{
    public string ResourceGroupName { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Region { get; init; } = string.Empty;
    public string KubernetesVersion { get; init; } = string.Empty;
    public int NodeCount { get; init; }
    public string VmSize { get; init; } = string.Empty;
    public string ApiServerUrl { get; init; } = string.Empty;
}

/// <summary>
/// AKS service deployment information
/// </summary>
public record AKSServiceDeploymentInfo
{
    public string ClusterName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DeployedAt { get; init; }
    public string ImageName { get; init; } = string.Empty;
    public int Replicas { get; init; }
    public int Port { get; init; }
    public int TargetPort { get; init; }
    public string ServiceUrl { get; init; } = string.Empty;
}

/// <summary>
/// AKS cluster information
/// </summary>
public record AKSClusterInfo
{
    public string Name { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string KubernetesVersion { get; init; } = string.Empty;
    public string ApiServerUrl { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public int NodeCount { get; init; }
    public string VmSize { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public List<string> NodePools { get; init; } = new();
}

/// <summary>
/// AKS service information
/// </summary>
public record AKSServiceInfo
{
    public string Name { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Port { get; init; }
    public int TargetPort { get; init; }
    public string ClusterIP { get; init; } = string.Empty;
    public string ExternalIP { get; init; } = string.Empty;
    public int Replicas { get; init; }
    public int AvailableReplicas { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public Dictionary<string, string> Labels { get; init; } = new();
    public Dictionary<string, string> Annotations { get; init; } = new();
}

/// <summary>
/// AKS pod information
/// </summary>
public record AKSPodInfo
{
    public string Name { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    public string NodeName { get; init; } = string.Empty;
    public string PodIP { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public List<string> Containers { get; init; } = new();
    public Dictionary<string, string> Labels { get; init; } = new();
    public Dictionary<string, string> Annotations { get; init; } = new();
}

/// <summary>
/// AKS pod log entry
/// </summary>
public record AKSPodLogEntry
{
    public string PodName { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// AKS command execution result
/// </summary>
public record AKSCommandResult
{
    public string PodName { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public int ExitCode { get; init; }
    public DateTime ExecutedAt { get; init; }
    public TimeSpan ExecutionTime { get; init; }
}

/// <summary>
/// AKS cluster credentials
/// </summary>
public record AKSClusterCredentials
{
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string KubeConfig { get; init; } = string.Empty;
    public string ApiServerUrl { get; init; } = string.Empty;
    public string CertificateAuthorityData { get; init; } = string.Empty;
    public string ClientCertificateData { get; init; } = string.Empty;
    public string ClientKeyData { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// AKS cluster configuration
/// </summary>
public record AKSClusterConfiguration
{
    public string? KubernetesVersion { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
    public Dictionary<string, string> AddOnProfiles { get; init; } = new();
    public Dictionary<string, object> NetworkProfile { get; init; } = new();
    public Dictionary<string, object> IdentityProfile { get; init; } = new();
}

/// <summary>
/// AKS node pool information
/// </summary>
public record AKSNodePoolInfo
{
    public string Name { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string VmSize { get; init; } = string.Empty;
    public int NodeCount { get; init; }
    public int MinNodeCount { get; init; }
    public int MaxNodeCount { get; init; }
    public bool EnableAutoScaling { get; init; }
    public string OsType { get; init; } = string.Empty;
    public string OsDiskSizeGB { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
    public Dictionary<string, string> NodeLabels { get; init; } = new();
    public List<string> NodeTaints { get; init; } = new();
} 
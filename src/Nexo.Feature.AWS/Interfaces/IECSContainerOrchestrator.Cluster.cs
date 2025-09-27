using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// ECS cluster management functionality
    /// </summary>
    public partial interface IECSContainerOrchestrator
    {
        /// <summary>
        /// Creates an ECS cluster
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="capacityProviders">Capacity providers</param>
        /// <param name="defaultCapacityProviderStrategy">Default capacity provider strategy</param>
        /// <param name="tags">Resource tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cluster creation result</returns>
        Task<ECSClusterResult> CreateClusterAsync(
            string clusterName,
            List<string>? capacityProviders = null,
            List<CapacityProviderStrategyItem>? defaultCapacityProviderStrategy = null,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an ECS cluster
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="forceDelete">Force delete even if cluster contains services</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cluster deletion result</returns>
        Task<ECSClusterResult> DeleteClusterAsync(
            string clusterName,
            bool forceDelete = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets ECS cluster information
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cluster information</returns>
        Task<ECSClusterInfo> GetClusterInfoAsync(string clusterName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all ECS clusters
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clusters</returns>
        Task<ECSClusterListResult> ListClustersAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Capacity provider strategy item
    /// </summary>
    public partial class CapacityProviderStrategyItem
    {
        /// <summary>
        /// Capacity provider name
        /// </summary>
        public string CapacityProvider { get; set; } = string.Empty;

        /// <summary>
        /// Weight for the capacity provider
        /// </summary>
        public int Weight { get; set; } = 1;

        /// <summary>
        /// Base value for the capacity provider
        /// </summary>
        public int Base { get; set; } = 0;
    }

    /// <summary>
    /// ECS cluster result
    /// </summary>
    public partial class ECSClusterResult
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
        /// Cluster name
        /// </summary>
        public string ClusterName { get; set; } = string.Empty;

        /// <summary>
        /// Cluster ARN
        /// </summary>
        public string? ClusterArn { get; set; }

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
    /// ECS cluster information
    /// </summary>
    public partial class ECSClusterInfo
    {
        /// <summary>
        /// Cluster name
        /// </summary>
        public string ClusterName { get; set; } = string.Empty;

        /// <summary>
        /// Cluster ARN
        /// </summary>
        public string ClusterArn { get; set; } = string.Empty;

        /// <summary>
        /// Cluster status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Active services count
        /// </summary>
        public int ActiveServicesCount { get; set; }

        /// <summary>
        /// Running tasks count
        /// </summary>
        public int RunningTasksCount { get; set; }

        /// <summary>
        /// Pending tasks count
        /// </summary>
        public int PendingTasksCount { get; set; }

        /// <summary>
        /// Registered container instances count
        /// </summary>
        public int RegisteredContainerInstancesCount { get; set; }

        /// <summary>
        /// Capacity providers
        /// </summary>
        public List<string> CapacityProviders { get; set; } = new List<string>();

        /// <summary>
        /// Default capacity provider strategy
        /// </summary>
        public List<CapacityProviderStrategyItem> DefaultCapacityProviderStrategy { get; set; } = new List<CapacityProviderStrategyItem>();

        /// <summary>
        /// Cluster creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// ECS cluster list result
    /// </summary>
    public partial class ECSClusterListResult
    {
        /// <summary>
        /// Whether the list operation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// List message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// List of clusters
        /// </summary>
        public List<ECSClusterInfo> Clusters { get; set; } = new List<ECSClusterInfo>();

        /// <summary>
        /// List timestamp
        /// </summary>
        public DateTime ListedAt { get; set; }

        /// <summary>
        /// Error details if list failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }
}

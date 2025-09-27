using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// ECS service management functionality
    /// </summary>
    public partial interface IECSContainerOrchestrator
    {
        /// <summary>
        /// Creates an ECS service
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="taskDefinition">Task definition ARN</param>
        /// <param name="desiredCount">Desired number of tasks</param>
        /// <param name="launchType">Launch type (EC2, FARGATE)</param>
        /// <param name="subnets">Subnet IDs</param>
        /// <param name="securityGroups">Security group IDs</param>
        /// <param name="assignPublicIp">Whether to assign public IP</param>
        /// <param name="loadBalancer">Load balancer configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service creation result</returns>
        Task<ECSServiceResult> CreateServiceAsync(
            string clusterName,
            string serviceName,
            string taskDefinition,
            int desiredCount,
            string launchType,
            List<string>? subnets = null,
            List<string>? securityGroups = null,
            bool assignPublicIp = false,
            LoadBalancerConfig? loadBalancer = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an ECS service
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="taskDefinition">New task definition ARN</param>
        /// <param name="desiredCount">New desired count</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service update result</returns>
        Task<ECSServiceResult> UpdateServiceAsync(
            string clusterName,
            string serviceName,
            string? taskDefinition = null,
            int? desiredCount = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an ECS service
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="forceDelete">Force delete even if service is running</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service deletion result</returns>
        Task<ECSServiceResult> DeleteServiceAsync(
            string clusterName,
            string serviceName,
            bool forceDelete = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets ECS service information
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="serviceName">Service name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service information</returns>
        Task<ECSServiceInfo> GetServiceInfoAsync(string clusterName, string serviceName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists services in a cluster
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of services</returns>
        Task<ECSServiceListResult> ListServicesAsync(string clusterName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Load balancer configuration
    /// </summary>
    public partial class LoadBalancerConfig
    {
        /// <summary>
        /// Load balancer ARN
        /// </summary>
        public string LoadBalancerArn { get; set; } = string.Empty;

        /// <summary>
        /// Target group ARN
        /// </summary>
        public string TargetGroupArn { get; set; } = string.Empty;

        /// <summary>
        /// Container name
        /// </summary>
        public string ContainerName { get; set; } = string.Empty;

        /// <summary>
        /// Container port
        /// </summary>
        public int ContainerPort { get; set; }
    }

    /// <summary>
    /// ECS service result
    /// </summary>
    public partial class ECSServiceResult
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
        /// Service name
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Service ARN
        /// </summary>
        public string? ServiceArn { get; set; }

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
    /// ECS service information
    /// </summary>
    public partial class ECSServiceInfo
    {
        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Service ARN
        /// </summary>
        public string ServiceArn { get; set; } = string.Empty;

        /// <summary>
        /// Cluster ARN
        /// </summary>
        public string ClusterArn { get; set; } = string.Empty;

        /// <summary>
        /// Task definition ARN
        /// </summary>
        public string TaskDefinition { get; set; } = string.Empty;

        /// <summary>
        /// Service status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Desired count
        /// </summary>
        public int DesiredCount { get; set; }

        /// <summary>
        /// Running count
        /// </summary>
        public int RunningCount { get; set; }

        /// <summary>
        /// Pending count
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Launch type
        /// </summary>
        public string LaunchType { get; set; } = string.Empty;

        /// <summary>
        /// Service creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Service last updated date
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// ECS service list result
    /// </summary>
    public partial class ECSServiceListResult
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
        /// List of services
        /// </summary>
        public List<ECSServiceInfo> Services { get; set; } = new List<ECSServiceInfo>();

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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// ECS container orchestration interface
    /// </summary>
    public interface IECSContainerOrchestrator
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

        /// <summary>
        /// Runs a one-time ECS task
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="taskDefinition">Task definition ARN</param>
        /// <param name="launchType">Launch type</param>
        /// <param name="subnets">Subnet IDs</param>
        /// <param name="securityGroups">Security group IDs</param>
        /// <param name="assignPublicIp">Whether to assign public IP</param>
        /// <param name="overrides">Task overrides</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task run result</returns>
        Task<ECSTaskResult> RunTaskAsync(
            string clusterName,
            string taskDefinition,
            string launchType,
            List<string>? subnets = null,
            List<string>? securityGroups = null,
            bool assignPublicIp = false,
            TaskOverride? overrides = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops an ECS task
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="taskArn">Task ARN</param>
        /// <param name="reason">Stop reason</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task stop result</returns>
        Task<ECSTaskResult> StopTaskAsync(
            string clusterName,
            string taskArn,
            string reason = "User requested stop",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets ECS task information
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="taskArn">Task ARN</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task information</returns>
        Task<ECSTaskInfo> GetTaskInfoAsync(string clusterName, string taskArn, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists tasks in a cluster
        /// </summary>
        /// <param name="clusterName">Cluster name</param>
        /// <param name="serviceName">Optional service name filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of tasks</returns>
        Task<ECSTaskListResult> ListTasksAsync(string clusterName, string? serviceName = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Capacity provider strategy item
    /// </summary>
    public class CapacityProviderStrategyItem
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
    /// Load balancer configuration
    /// </summary>
    public class LoadBalancerConfig
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
    /// Task override configuration
    /// </summary>
    public class TaskOverride
    {
        /// <summary>
        /// Container overrides
        /// </summary>
        public List<ContainerOverride> ContainerOverrides { get; set; } = new List<ContainerOverride>();

        /// <summary>
        /// Task role ARN
        /// </summary>
        public string? TaskRoleArn { get; set; }

        /// <summary>
        /// Execution role ARN
        /// </summary>
        public string? ExecutionRoleArn { get; set; }
    }

    /// <summary>
    /// Container override configuration
    /// </summary>
    public class ContainerOverride
    {
        /// <summary>
        /// Container name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Command override
        /// </summary>
        public List<string>? Command { get; set; }

        /// <summary>
        /// Environment variables
        /// </summary>
        public List<KeyValuePair<string, string>>? Environment { get; set; }

        /// <summary>
        /// CPU override
        /// </summary>
        public int? Cpu { get; set; }

        /// <summary>
        /// Memory override
        /// </summary>
        public int? Memory { get; set; }
    }

    /// <summary>
    /// ECS cluster result
    /// </summary>
    public class ECSClusterResult
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
    public class ECSClusterInfo
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
    public class ECSClusterListResult
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

    /// <summary>
    /// ECS service result
    /// </summary>
    public class ECSServiceResult
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
    public class ECSServiceInfo
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
    public class ECSServiceListResult
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

    /// <summary>
    /// ECS task result
    /// </summary>
    public class ECSTaskResult
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
        /// Task ARN
        /// </summary>
        public string? TaskArn { get; set; }

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
    /// ECS task information
    /// </summary>
    public class ECSTaskInfo
    {
        /// <summary>
        /// Task ARN
        /// </summary>
        public string TaskArn { get; set; } = string.Empty;

        /// <summary>
        /// Cluster ARN
        /// </summary>
        public string ClusterArn { get; set; } = string.Empty;

        /// <summary>
        /// Task definition ARN
        /// </summary>
        public string TaskDefinitionArn { get; set; } = string.Empty;

        /// <summary>
        /// Task status
        /// </summary>
        public string LastStatus { get; set; } = string.Empty;

        /// <summary>
        /// Desired status
        /// </summary>
        public string DesiredStatus { get; set; } = string.Empty;

        /// <summary>
        /// Launch type
        /// </summary>
        public string LaunchType { get; set; } = string.Empty;

        /// <summary>
        /// Task creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Task start date
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Task stop date
        /// </summary>
        public DateTime? StoppedAt { get; set; }

        /// <summary>
        /// Stop reason
        /// </summary>
        public string? StoppedReason { get; set; }

        /// <summary>
        /// Container information
        /// </summary>
        public List<ContainerInfo> Containers { get; set; } = new List<ContainerInfo>();
    }

    /// <summary>
    /// Container information
    /// </summary>
    public class ContainerInfo
    {
        /// <summary>
        /// Container name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Container ARN
        /// </summary>
        public string ContainerArn { get; set; } = string.Empty;

        /// <summary>
        /// Container status
        /// </summary>
        public string LastStatus { get; set; } = string.Empty;

        /// <summary>
        /// Container exit code
        /// </summary>
        public int? ExitCode { get; set; }

        /// <summary>
        /// Container reason
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Container start date
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Container stop date
        /// </summary>
        public DateTime? FinishedAt { get; set; }
    }

    /// <summary>
    /// ECS task list result
    /// </summary>
    public class ECSTaskListResult
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
        /// List of tasks
        /// </summary>
        public List<ECSTaskInfo> Tasks { get; set; } = new List<ECSTaskInfo>();

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
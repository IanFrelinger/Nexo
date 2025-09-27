using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// ECS task management functionality
    /// </summary>
    public partial interface IECSContainerOrchestrator
    {
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

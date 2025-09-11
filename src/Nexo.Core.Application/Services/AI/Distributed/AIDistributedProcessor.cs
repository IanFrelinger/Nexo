using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Distributed
{
    /// <summary>
    /// Distributed AI processing service for multi-device coordination
    /// </summary>
    public class AIDistributedProcessor
    {
        private readonly ILogger<AIDistributedProcessor> _logger;
        private readonly Dictionary<string, ProcessingNode> _nodes;
        private readonly Dictionary<string, DistributedTask> _tasks;
        private readonly object _lockObject = new object();

        public AIDistributedProcessor(ILogger<AIDistributedProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodes = new Dictionary<string, ProcessingNode>();
            _tasks = new Dictionary<string, DistributedTask>();
        }

        /// <summary>
        /// Registers a processing node
        /// </summary>
        public async Task<bool> RegisterNodeAsync(NodeRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation("Registering processing node {NodeId} with capabilities {Capabilities}", 
                    request.NodeId, string.Join(", ", request.Capabilities));

                var node = new ProcessingNode
                {
                    NodeId = request.NodeId,
                    Name = request.Name,
                    Capabilities = request.Capabilities,
                    Status = NodeStatus.Available,
                    LastHeartbeat = DateTime.UtcNow,
                    ResourceInfo = request.ResourceInfo,
                    Location = request.Location
                };

                lock (_lockObject)
                {
                    _nodes[request.NodeId] = node;
                }

                _logger.LogInformation("Processing node {NodeId} registered successfully", request.NodeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register processing node {NodeId}", request.NodeId);
                return false;
            }
        }

        /// <summary>
        /// Unregisters a processing node
        /// </summary>
        public async Task<bool> UnregisterNodeAsync(string nodeId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_nodes.ContainsKey(nodeId))
                    {
                        _nodes.Remove(nodeId);
                        _logger.LogInformation("Processing node {NodeId} unregistered", nodeId);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister processing node {NodeId}", nodeId);
                return false;
            }
        }

        /// <summary>
        /// Updates node heartbeat
        /// </summary>
        public async Task<bool> UpdateNodeHeartbeatAsync(string nodeId, NodeResourceInfo resourceInfo)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_nodes.TryGetValue(nodeId, out var node))
                    {
                        node.LastHeartbeat = DateTime.UtcNow;
                        node.ResourceInfo = resourceInfo;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update heartbeat for node {NodeId}", nodeId);
                return false;
            }
        }

        /// <summary>
        /// Submits a distributed task
        /// </summary>
        public async Task<DistributedTask> SubmitTaskAsync(DistributedTaskRequest request)
        {
            try
            {
                _logger.LogInformation("Submitting distributed task {TaskType} with {SubTaskCount} sub-tasks", 
                    request.TaskType, request.SubTasks.Count);

                var task = new DistributedTask
                {
                    TaskId = Guid.NewGuid().ToString(),
                    Request = request,
                    Status = TaskStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    SubTasks = new List<SubTask>(),
                    Results = new List<TaskResult>()
                };

                // Create sub-tasks
                foreach (var subTaskRequest in request.SubTasks)
                {
                    var subTask = new SubTask
                    {
                        SubTaskId = Guid.NewGuid().ToString(),
                        ParentTaskId = task.TaskId,
                        Request = subTaskRequest,
                        Status = SubTaskStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    task.SubTasks.Add(subTask);
                }

                lock (_lockObject)
                {
                    _tasks[task.TaskId] = task;
                }

                // Start task processing
                _ = Task.Run(() => ProcessDistributedTaskAsync(task));

                _logger.LogInformation("Distributed task {TaskId} submitted successfully", task.TaskId);
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit distributed task");
                throw;
            }
        }

        /// <summary>
        /// Gets task status
        /// </summary>
        public async Task<DistributedTask?> GetTaskStatusAsync(string taskId)
        {
            try
            {
                lock (_lockObject)
                {
                    _tasks.TryGetValue(taskId, out var task);
                    return task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get task status for {TaskId}", taskId);
                return null;
            }
        }

        /// <summary>
        /// Cancels a distributed task
        /// </summary>
        public async Task<bool> CancelTaskAsync(string taskId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_tasks.TryGetValue(taskId, out var task))
                    {
                        task.Status = TaskStatus.Cancelled;
                        task.CompletedAt = DateTime.UtcNow;
                        
                        // Cancel all sub-tasks
                        foreach (var subTask in task.SubTasks.Where(st => st.Status == SubTaskStatus.Running))
                        {
                            subTask.Status = SubTaskStatus.Cancelled;
                            subTask.CompletedAt = DateTime.UtcNow;
                        }
                        
                        _logger.LogInformation("Distributed task {TaskId} cancelled", taskId);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel task {TaskId}", taskId);
                return false;
            }
        }

        /// <summary>
        /// Gets all available nodes
        /// </summary>
        public async Task<List<ProcessingNode>> GetAvailableNodesAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    return _nodes.Values
                        .Where(n => n.Status == NodeStatus.Available && 
                                   DateTime.UtcNow - n.LastHeartbeat < TimeSpan.FromMinutes(5))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available nodes");
                return new List<ProcessingNode>();
            }
        }

        /// <summary>
        /// Gets task distribution statistics
        /// </summary>
        public async Task<DistributionStatistics> GetDistributionStatisticsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    var statistics = new DistributionStatistics
                    {
                        TotalNodes = _nodes.Count,
                        AvailableNodes = _nodes.Values.Count(n => n.Status == NodeStatus.Available),
                        BusyNodes = _nodes.Values.Count(n => n.Status == NodeStatus.Busy),
                        OfflineNodes = _nodes.Values.Count(n => n.Status == NodeStatus.Offline),
                        TotalTasks = _tasks.Count,
                        PendingTasks = _tasks.Values.Count(t => t.Status == TaskStatus.Pending),
                        RunningTasks = _tasks.Values.Count(t => t.Status == TaskStatus.Running),
                        CompletedTasks = _tasks.Values.Count(t => t.Status == TaskStatus.Completed),
                        FailedTasks = _tasks.Values.Count(t => t.Status == TaskStatus.Failed),
                        GeneratedAt = DateTime.UtcNow
                    };

                    return statistics;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get distribution statistics");
                throw;
            }
        }

        private async Task ProcessDistributedTaskAsync(DistributedTask task)
        {
            try
            {
                _logger.LogInformation("Processing distributed task {TaskId}", task.TaskId);

                task.Status = TaskStatus.Running;
                task.StartedAt = DateTime.UtcNow;

                // Process sub-tasks in parallel
                var subTaskTasks = task.SubTasks.Select(subTask => ProcessSubTaskAsync(subTask)).ToArray();
                await Task.WhenAll(subTaskTasks);

                // Determine overall task status
                var completedSubTasks = task.SubTasks.Count(st => st.Status == SubTaskStatus.Completed);
                var failedSubTasks = task.SubTasks.Count(st => st.Status == SubTaskStatus.Failed);

                if (completedSubTasks == task.SubTasks.Count)
                {
                    task.Status = TaskStatus.Completed;
                }
                else if (failedSubTasks > 0)
                {
                    task.Status = TaskStatus.Failed;
                }
                else
                {
                    task.Status = TaskStatus.PartiallyCompleted;
                }

                task.CompletedAt = DateTime.UtcNow;
                task.Duration = task.CompletedAt.Value - task.StartedAt.Value;

                _logger.LogInformation("Distributed task {TaskId} completed with status {Status} in {Duration}ms", 
                    task.TaskId, task.Status, task.Duration?.TotalMilliseconds ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Distributed task {TaskId} failed", task.TaskId);
                task.Status = TaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.CompletedAt = DateTime.UtcNow;
            }
        }

        private async Task ProcessSubTaskAsync(SubTask subTask)
        {
            try
            {
                _logger.LogDebug("Processing sub-task {SubTaskId}", subTask.SubTaskId);

                subTask.Status = SubTaskStatus.Running;
                subTask.StartedAt = DateTime.UtcNow;

                // Find suitable node for sub-task
                var node = await FindSuitableNodeAsync(subTask.Request);
                if (node == null)
                {
                    subTask.Status = SubTaskStatus.Failed;
                    subTask.ErrorMessage = "No suitable node available";
                    subTask.CompletedAt = DateTime.UtcNow;
                    return;
                }

                // Assign node to sub-task
                subTask.AssignedNodeId = node.NodeId;
                node.Status = NodeStatus.Busy;

                // Simulate sub-task processing
                await SimulateSubTaskProcessingAsync(subTask);

                // Complete sub-task
                subTask.Status = SubTaskStatus.Completed;
                subTask.CompletedAt = DateTime.UtcNow;
                subTask.Duration = subTask.CompletedAt.Value - subTask.StartedAt.Value;

                // Release node
                node.Status = NodeStatus.Available;

                _logger.LogDebug("Sub-task {SubTaskId} completed successfully", subTask.SubTaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sub-task {SubTaskId} failed", subTask.SubTaskId);
                subTask.Status = SubTaskStatus.Failed;
                subTask.ErrorMessage = ex.Message;
                subTask.CompletedAt = DateTime.UtcNow;
            }
        }

        private async Task<ProcessingNode?> FindSuitableNodeAsync(SubTaskRequest request)
        {
            var availableNodes = await GetAvailableNodesAsync();
            
            // Find node with required capabilities
            var suitableNodes = availableNodes.Where(n => 
                n.Capabilities.Contains(request.RequiredCapability) &&
                n.ResourceInfo.CpuUsage < 80 &&
                n.ResourceInfo.MemoryUsage < 80).ToList();

            if (!suitableNodes.Any())
                return null;

            // Select node with lowest resource usage
            return suitableNodes.OrderBy(n => n.ResourceInfo.CpuUsage + n.ResourceInfo.MemoryUsage).First();
        }

        private async Task SimulateSubTaskProcessingAsync(SubTask subTask)
        {
            // Simulate processing time based on task complexity
            var processingTime = subTask.Request.Complexity switch
            {
                TaskComplexity.Low => Random.Shared.Next(1000, 3000),
                TaskComplexity.Medium => Random.Shared.Next(3000, 8000),
                TaskComplexity.High => Random.Shared.Next(8000, 15000),
                _ => Random.Shared.Next(1000, 5000)
            };

            await Task.Delay(processingTime);

            // Simulate result generation
            subTask.Result = new TaskResult
            {
                ResultId = Guid.NewGuid().ToString(),
                SubTaskId = subTask.SubTaskId,
                Success = true,
                Data = $"Processed result for {subTask.Request.OperationType}",
                ProcessingTime = TimeSpan.FromMilliseconds(processingTime),
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Node registration request
    /// </summary>
    public class NodeRegistrationRequest
    {
        public string NodeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public NodeResourceInfo ResourceInfo { get; set; } = new();
        public NodeLocation Location { get; set; } = new();
    }

    /// <summary>
    /// Processing node
    /// </summary>
    public class ProcessingNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public NodeStatus Status { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public NodeResourceInfo ResourceInfo { get; set; } = new();
        public NodeLocation Location { get; set; } = new();
    }

    /// <summary>
    /// Node resource information
    /// </summary>
    public class NodeResourceInfo
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public int AvailableCores { get; set; }
        public long AvailableMemory { get; set; }
        public long AvailableDisk { get; set; }
    }

    /// <summary>
    /// Node location
    /// </summary>
    public class NodeLocation
    {
        public string Region { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string DataCenter { get; set; } = string.Empty;
        public GeoLocation? GeoLocation { get; set; }
    }

    /// <summary>
    /// Geographic location
    /// </summary>
    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    /// <summary>
    /// Distributed task request
    /// </summary>
    public class DistributedTaskRequest
    {
        public string TaskType { get; set; } = string.Empty;
        public List<SubTaskRequest> SubTasks { get; set; } = new();
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Sub-task request
    /// </summary>
    public class SubTaskRequest
    {
        public string OperationType { get; set; } = string.Empty;
        public string RequiredCapability { get; set; } = string.Empty;
        public TaskComplexity Complexity { get; set; } = TaskComplexity.Medium;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Distributed task
    /// </summary>
    public class DistributedTask
    {
        public string TaskId { get; set; } = string.Empty;
        public DistributedTaskRequest Request { get; set; } = new();
        public TaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public List<SubTask> SubTasks { get; set; } = new();
        public List<TaskResult> Results { get; set; } = new();
    }

    /// <summary>
    /// Sub-task
    /// </summary>
    public class SubTask
    {
        public string SubTaskId { get; set; } = string.Empty;
        public string ParentTaskId { get; set; } = string.Empty;
        public SubTaskRequest Request { get; set; } = new();
        public SubTaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? AssignedNodeId { get; set; }
        public string? ErrorMessage { get; set; }
        public TaskResult? Result { get; set; }
    }

    /// <summary>
    /// Task result
    /// </summary>
    public class TaskResult
    {
        public string ResultId { get; set; } = string.Empty;
        public string SubTaskId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Data { get; set; } = string.Empty;
        public TimeSpan ProcessingTime { get; set; }
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Distribution statistics
    /// </summary>
    public class DistributionStatistics
    {
        public int TotalNodes { get; set; }
        public int AvailableNodes { get; set; }
        public int BusyNodes { get; set; }
        public int OfflineNodes { get; set; }
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int RunningTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int FailedTasks { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    // Enums
    public enum NodeStatus { Available, Busy, Offline, Maintenance }
    public enum TaskStatus { Pending, Running, Completed, Failed, Cancelled, PartiallyCompleted }
    public enum SubTaskStatus { Pending, Running, Completed, Failed, Cancelled }
    public enum TaskPriority { Low, Normal, High, Critical }
    public enum TaskComplexity { Low, Medium, High }
}

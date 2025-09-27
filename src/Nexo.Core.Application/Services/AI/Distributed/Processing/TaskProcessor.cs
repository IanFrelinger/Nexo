using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.AI.Distributed.Models;

namespace Nexo.Core.Application.Services.AI.Distributed.Processing
{
    /// <summary>
    /// Processes distributed tasks for AI processing
    /// </summary>
    public class TaskProcessor
    {
        private readonly ILogger _logger;
        private readonly NodeManager _nodeManager;
        private readonly Dictionary<string, DistributedTask> _tasks;
        private readonly object _lockObject = new object();

        public TaskProcessor(ILogger logger, NodeManager nodeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodeManager = nodeManager ?? throw new ArgumentNullException(nameof(nodeManager));
            _tasks = new Dictionary<string, DistributedTask>();
        }

        /// <summary>
        /// Submits a distributed task
        /// </summary>
        public Task<DistributedTask> SubmitTaskAsync(DistributedTaskRequest request)
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
                return Task.FromResult(task);
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
        public Task<DistributedTask?> GetTaskStatusAsync(string taskId)
        {
            try
            {
                lock (_lockObject)
                {
                    _tasks.TryGetValue(taskId, out var task);
                    return Task.FromResult(task);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get task status for {TaskId}", taskId);
                return Task.FromResult<DistributedTask?>(null);
            }
        }

        /// <summary>
        /// Cancels a distributed task
        /// </summary>
        public Task<bool> CancelTaskAsync(string taskId)
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
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel task {TaskId}", taskId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Gets all tasks
        /// </summary>
        public List<DistributedTask> GetAllTasks()
        {
            lock (_lockObject)
            {
                return _tasks.Values.ToList();
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
            var availableNodes = await _nodeManager.GetAvailableNodesAsync();
            
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
}

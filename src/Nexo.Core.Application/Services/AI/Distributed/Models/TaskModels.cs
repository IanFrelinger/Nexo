using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Services.AI.Distributed.Models
{
    /// <summary>
    /// Distributed task request
    /// </summary>
    public partial class DistributedTaskRequest
    {
        public string TaskType { get; set; } = string.Empty;
        public List<SubTaskRequest> SubTasks { get; set; } = new();
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Sub-task request
    /// </summary>
    public partial class SubTaskRequest
    {
        public string OperationType { get; set; } = string.Empty;
        public string RequiredCapability { get; set; } = string.Empty;
        public TaskComplexity Complexity { get; set; } = TaskComplexity.Medium;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Distributed task
    /// </summary>
    public partial class DistributedTask
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
    public partial class SubTask
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
    public partial class TaskResult
    {
        public string ResultId { get; set; } = string.Empty;
        public string SubTaskId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Data { get; set; } = string.Empty;
        public TimeSpan ProcessingTime { get; set; }
        public DateTime GeneratedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}

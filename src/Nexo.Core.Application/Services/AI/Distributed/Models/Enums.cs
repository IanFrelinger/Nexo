namespace Nexo.Core.Application.Services.AI.Distributed.Models
{
    // Enums
    public enum NodeStatus { Available, Busy, Offline, Maintenance }
    public enum TaskStatus { Pending, Running, Completed, Failed, Cancelled, PartiallyCompleted }
    public enum SubTaskStatus { Pending, Running, Completed, Failed, Cancelled }
    public enum TaskPriority { Low, Normal, High, Critical }
    public enum TaskComplexity { Low, Medium, High }
}

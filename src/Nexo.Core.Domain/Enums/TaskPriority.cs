namespace Nexo.Core.Domain.Enums
{
    /// <summary>
    /// Represents the priority level of a task, indicating its relative importance or urgency.
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>
        /// Indicates a low-priority level for a task, suggesting it is not urgent and can be addressed after higher-priority tasks.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Indicates a medium priority level for a task.
        /// Tasks with this priority are considered important but not urgent.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Indicates tasks with a high level of priority that should be addressed before tasks with lower priorities.
        /// </summary>
        High = 2,

        /// <summary>
        /// Represents the highest level of urgency and importance for a task, requiring immediate attention and resolution.
        /// </summary>
        Critical = 3
    }
}
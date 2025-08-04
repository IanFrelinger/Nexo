namespace Nexo.Core.Domain.Enums
{
    /// <summary>
    /// Defines various statuses that a task can have, indicating its current state in the workflow.
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// Indicates that a task is yet to be started or worked on.
        /// This is the initial state of a task.
        /// </summary>
        Todo = 0,

        /// <summary>
        /// Indicates that a task is currently being worked on and is in progress.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Indicates that a task has been completed and meets all the defined criteria for being considered done.
        /// </summary>
        Done = 2,

        /// <summary>
        /// Indicates that a task is currently blocked and cannot proceed due to dependencies, issues, or other blockers.
        /// </summary>
        Blocked = 3
    }
}
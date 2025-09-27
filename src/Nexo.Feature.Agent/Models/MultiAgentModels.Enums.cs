using System;

namespace Nexo.Feature.Agent.Models
{
    /// <summary>
    /// Enums and enumeration types for multi-agent systems
    /// </summary>
    public partial class MultiAgentModels
    {
        // Enums are defined in separate files
    }

    /// <summary>
    /// Collaboration session types.
    /// </summary>
    public enum CollaborationSessionType
    {
        /// <summary>
        /// General collaboration session.
        /// </summary>
        General,

        /// <summary>
        /// Task execution session.
        /// </summary>
        TaskExecution,

        /// <summary>
        /// Code review session.
        /// </summary>
        CodeReview
    }

    /// <summary>
    /// Collaboration session status.
    /// </summary>
    public enum CollaborationSessionStatus
    {
        /// <summary>
        /// Session has been created.
        /// </summary>
        Created,

        /// <summary>
        /// Session is active and running.
        /// </summary>
        Active,

        /// <summary>
        /// Session has been completed.
        /// </summary>
        Completed
    }

    /// <summary>
    /// Task priority levels.
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>
        /// Normal priority task.
        /// </summary>
        Normal,

        /// <summary>
        /// High-priority task.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority task.
        /// </summary>
        Critical
    }
}

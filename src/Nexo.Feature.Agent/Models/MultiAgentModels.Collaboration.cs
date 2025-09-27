using System;
using System.Collections.Generic;
using Nexo.Feature.Agent.Interfaces;

namespace Nexo.Feature.Agent.Models
{
    /// <summary>
    /// Collaboration-related models and data structures
    /// </summary>
    public partial class MultiAgentModels
    {
        // Collaboration models are defined in separate files
    }

    /// <summary>
    /// Request to create a collaboration session between multiple agents.
    /// </summary>
    public class CollaborationRequest
    {
        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        public string SessionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of collaboration session.
        /// </summary>
        public CollaborationSessionType SessionType { get; set; } = CollaborationSessionType.General;

        /// <summary>
        /// Gets or sets the required capabilities for participating agents.
        /// </summary>
        public List<string> RequiredCapabilities { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the required roles for participating agents.
        /// </summary>
        public List<string> RequiredRoles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether AI capabilities are required.
        /// </summary>
        public bool RequireAiCapabilities { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of agents to include.
        /// </summary>
        public int MaxAgents { get; set; } = 5;

        /// <summary>
        /// Gets or sets the session configuration.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Collaboration session between multiple agents.
    /// </summary>
    public class CollaborationSession
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        public string SessionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the participating agents.
        /// </summary>
        public List<IAiEnhancedAgent> ParticipatingAgents { get; set; } = new List<IAiEnhancedAgent>();

        /// <summary>
        /// Gets or sets the session type.
        /// </summary>
        public CollaborationSessionType SessionType { get; set; } = CollaborationSessionType.General;

        /// <summary>
        /// Gets or sets the session status.
        /// </summary>
        public CollaborationSessionStatus Status { get; set; } = CollaborationSessionStatus.Created;

        /// <summary>
        /// Gets or sets when the session was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when the session was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the session configuration.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Collaborative task to be executed by multiple agents.
    /// </summary>
    public class CollaborativeTask
    {
        /// <summary>
        /// Gets or sets the task name.
        /// </summary>
        public string TaskName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the task description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the task type.
        /// </summary>
        public string TaskType { get; set; } = "general";

        /// <summary>
        /// Gets or sets the required capabilities for the task.
        /// </summary>
        public List<string> RequiredCapabilities { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the required roles for the task.
        /// </summary>
        public List<string> RequiredRoles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the task priority.
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>
        /// Gets or sets the task complexity level.
        /// </summary>
        public int ComplexityLevel { get; set; } = 1;

        /// <summary>
        /// Gets or sets the task configuration.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }
}

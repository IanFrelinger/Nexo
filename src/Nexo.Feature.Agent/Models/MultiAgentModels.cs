using System;
using System.Collections.Generic;
using Nexo.Feature.Agent.Interfaces;
using System.Linq;

namespace Nexo.Feature.Agent.Models
{
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
        public bool RequireAICapabilities { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of agents to include.
        /// </summary>
        public int MaxAgents { get; set; } = 5;

        /// <summary>
        /// Gets or sets the session configuration.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the session metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
        public List<IAIEnhancedAgent> ParticipatingAgents { get; set; } = new List<IAIEnhancedAgent>();

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

        /// <summary>
        /// Gets or sets the session metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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

        /// <summary>
        /// Gets or sets the task metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of a collaborative task execution.
    /// </summary>
    public class CollaborationResult
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the task name.
        /// </summary>
        public string TaskName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the collaboration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the collaboration failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the individual agent results.
        /// </summary>
        public List<AgentTaskResult> AgentResults { get; set; } = new List<AgentTaskResult>();

        /// <summary>
        /// Gets or sets the synthesized result from all agents.
        /// </summary>
        public string SynthesizedResult { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collaboration metrics.
        /// </summary>
        public CollaborationMetrics CollaborationMetrics { get; set; } = new CollaborationMetrics();

        /// <summary>
        /// Gets or sets the collaboration metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of an individual agent's task execution.
    /// </summary>
    public class AgentTaskResult
    {
        /// <summary>
        /// Gets or sets the agent ID.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the agent name.
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the agent role.
        /// </summary>
        public string AgentRole { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the agent's task was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the agent's response content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if the agent's task failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets whether AI was used by the agent.
        /// </summary>
        public bool AIWasUsed { get; set; }

        /// <summary>
        /// Gets or sets the AI model used by the agent.
        /// </summary>
        public string AIModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confidence score of the agent's response.
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Gets or sets the agent's task metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Metrics for collaboration performance.
    /// </summary>
    public class CollaborationMetrics
    {
        /// <summary>
        /// Gets or sets the total processing time in milliseconds.
        /// </summary>
        public long TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the number of agents involved.
        /// </summary>
        public int AgentCount { get; set; }

        /// <summary>
        /// Gets or sets the average processing time per agent in milliseconds.
        /// </summary>
        public double AverageProcessingTimePerAgentMs => AgentCount > 0 ? (double)TotalProcessingTimeMs / AgentCount : 0;

        /// <summary>
        /// Gets or sets the success rate of agent tasks.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the total cost of the collaboration.
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// Gets or sets the average cost per agent.
        /// </summary>
        public decimal AverageCostPerAgent => AgentCount > 0 ? TotalCost / AgentCount : 0;
    }

    /// <summary>
    /// Request for agent-to-agent communication.
    /// </summary>
    public class AgentCommunicationRequest
    {
        /// <summary>
        /// Gets or sets the sender agent ID.
        /// </summary>
        public string SenderAgentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipient agent ID.
        /// </summary>
        public string RecipientAgentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public CommunicationMessageType MessageType { get; set; } = CommunicationMessageType.Information;

        /// <summary>
        /// Gets or sets the message priority.
        /// </summary>
        public CommunicationPriority Priority { get; set; } = CommunicationPriority.Normal;

        /// <summary>
        /// Gets or sets the communication metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of agent-to-agent communication.
    /// </summary>
    public class AgentCommunicationResult
    {
        /// <summary>
        /// Gets or sets the communication ID.
        /// </summary>
        public string CommunicationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the communication was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the response from the recipient agent.
        /// </summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if the communication failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets whether AI was used for processing.
        /// </summary>
        public bool AIWasUsed { get; set; }

        /// <summary>
        /// Gets or sets the AI model used for processing.
        /// </summary>
        public string AIModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the communication metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Analysis result of collaboration patterns.
    /// </summary>
    public class CollaborationAnalysisResult
    {
        /// <summary>
        /// Gets or sets whether the analysis was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the analysis failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the analysis timestamp.
        /// </summary>
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the number of active sessions.
        /// </summary>
        public int ActiveSessionsCount { get; set; }

        /// <summary>
        /// Gets or sets the number of completed sessions.
        /// </summary>
        public int CompletedSessionsCount { get; set; }

        /// <summary>
        /// Gets or sets the number of registered agents.
        /// </summary>
        public int RegisteredAgentsCount { get; set; }

        /// <summary>
        /// Gets or sets the agent collaboration patterns.
        /// </summary>
        public List<AgentCollaborationPattern> AgentCollaborationPatterns { get; set; } = new List<AgentCollaborationPattern>();

        /// <summary>
        /// Gets or sets the session performance metrics.
        /// </summary>
        public SessionPerformanceMetrics SessionPerformanceMetrics { get; set; } = new SessionPerformanceMetrics();

        /// <summary>
        /// Gets or sets the collaboration recommendations.
        /// </summary>
        public List<CollaborationRecommendation> Recommendations { get; set; } = new List<CollaborationRecommendation>();

        /// <summary>
        /// Gets or sets the analysis metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
        /// Problem solving session.
        /// </summary>
        ProblemSolving,

        /// <summary>
        /// Code review session.
        /// </summary>
        CodeReview,

        /// <summary>
        /// Architecture design session.
        /// </summary>
        ArchitectureDesign,

        /// <summary>
        /// Testing and validation session.
        /// </summary>
        TestingValidation
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
        Completed,

        /// <summary>
        /// Session has been cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Session has failed.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Task priority levels.
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>
        /// Low priority task.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority task.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority task.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority task.
        /// </summary>
        Critical
    }
} 
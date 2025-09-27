using System;
using System.Collections.Generic;

namespace Nexo.Feature.Agent.Models
{
    /// <summary>
    /// Communication and analysis models for multi-agent systems
    /// </summary>
    public partial class MultiAgentModels
    {
        // Communication models are defined in separate files
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
        public bool AiWasUsed { get; set; }

        /// <summary>
        /// Gets or sets the AI model used for processing.
        /// </summary>
        public string AiModelUsed { get; set; } = string.Empty;
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
    }
}

using System;
using System.Collections.Generic;
using Nexo.Feature.Agent.Interfaces;

namespace Nexo.Feature.Agent.Models
{
    /// <summary>
    /// Represents a potential agent for selection or collaboration.
    /// </summary>
    public class AgentCandidate
    {
        /// <summary>
        /// Represents an individual or entity authorized to perform specific actions on behalf of others or within a system.
        /// </summary>
        public IAiEnhancedAgent Agent { get; set; }

        /// <summary>
        /// Represents the score or points achieved during an event, game, or process.
        /// </summary>
        public double Score { get; set; }
    }

    /// <summary>
    /// Represents a profile describing an agent's capabilities, role, and areas of focus.
    /// </summary>
    public class AgentCapabilityProfile
    {
        /// <summary>
        /// Represents the unique identifier of an agent.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Specifies the name of the agent.
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Defines the role assigned to an agent within a specific context or operation.
        /// </summary>
        public string AgentRole { get; set; } = string.Empty;

        /// <summary>
        /// Describes the set of abilities or functionalities provided by an entity or system.
        /// </summary>
        public List<string> Capabilities { get; set; } = new List<string>();

        /// <summary>
        /// Defines the key areas of focus or interest.
        /// </summary>
        public List<string> FocusAreas { get; set; } = new List<string>();

        /// <summary>
        /// Represents the AI capabilities of an agent.
        /// </summary>
        public AiAgentCapabilities AiCapabilities { get; set; } = new AiAgentCapabilities();
    }

    /// <summary>
    /// Contextual information for agent communication.
    /// </summary>
    public class AgentCommunicationContext
    {
        /// <summary>
        /// Unique identifier for the communication session between agents.
        /// </summary>
        /// <remarks>
        /// This property is used to track and differentiate individual communication
        /// sessions within the agent system. It is typically generated as a
        /// GUID when a communication session is initiated.
        /// </remarks>
        public string CommunicationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender agent participating in the communication.
        /// </summary>
        /// <remarks>
        /// The SenderAgent property represents the initiating agent in a communication process.
        /// It implements the <see cref="IAiEnhancedAgent"/> interface, which enables AI-supported
        /// processing capabilities such as handling requests, generating suggestions, and analyzing tasks.
        /// This property is required to establish the context of the communication and facilitate AI-driven
        /// interactions if applicable.
        /// </remarks>
        public IAiEnhancedAgent SenderAgent { get; set; } = null;

        /// <summary>
        /// Represents the recipient agent in an agent-to-agent communication context.
        /// </summary>
        /// <remarks>
        /// The <c>RecipientAgent</c> property serves as the designated recipient for a communication
        /// within the agent framework, enabling interaction between AI-enhanced agents.
        /// It uses the <c>IAiEnhancedAgent</c> interface, which provides capabilities such as
        /// AI-based processing and task analysis.
        /// </remarks>
        public IAiEnhancedAgent RecipientAgent { get; set; } = null;

        /// <summary>
        /// Represents the content or text of a communication or notification.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Represents the type of the communication message being exchanged between agents.
        /// </summary>
        /// <remarks>
        /// The <see cref="MessageType"/> property is used to classify the purpose or nature of the communication.
        /// Common types include informational updates, questions, requests, responses, alerts, or coordination messages.
        /// This classification helps in processing and prioritizing messages effectively within the agent communication system.
        /// </remarks>
        public CommunicationMessageType MessageType { get; set; }

        /// <summary>
        /// Indicates the level of importance or urgency assigned to a task or item.
        /// </summary>
        public CommunicationPriority Priority { get; set; }

        /// <summary>
        /// Represents the timestamp of an event or entity, indicating the date and time it occurred or was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a collaboration pattern between agents.
    /// </summary>
    public class AgentCollaborationPattern
    {
        /// <summary>
        /// Identifies the unique identifier associated with a specific agent.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Represents the number of collaborations associated with an entity.
        /// </summary>
        public int CollaborationCount { get; set; }

        /// <summary>
        /// Defines the frequency at which collaboration occurs within a given context.
        /// </summary>
        public double CollaborationFrequency { get; set; }
    }

    /// <summary>
    /// Represents performance metrics for a session.
    /// </summary>
    public class SessionPerformanceMetrics
    {
        /// <summary>
        /// Indicates the average duration of a user session.
        /// </summary>
        public double AverageSessionDuration { get; set; }

        /// <summary>
        /// Indicates the average number of agents engaged per session.
        /// </summary>
        public double AverageAgentsPerSession { get; set; }

        /// <summary>
        /// Defines the success rate, typically represented as a percentage, indicating the effectiveness or completion rate of a specific process or operation.
        /// </summary>
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Represents a recommendation for collaboration between entities based on certain criteria or analysis.
    /// </summary>
    public class CollaborationRecommendation
    {
        /// <summary>
        /// Represents a classification or category of an object or entity.
        /// </summary>
        public RecommendationType Type { get; set; }

        /// <summary>
        /// A brief description of the collaboration recommendation.
        /// This property provides detailed information or context regarding the specific recommendation.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Indicates the priority level of a task or operation.
        /// </summary>
        public RecommendationPriority Priority { get; set; }

        /// <summary>
        /// Indicates the estimated impact or effect of a process, action, or event.
        /// </summary>
        public string EstimatedImpact { get; set; } = string.Empty;
    }


    /// <summary>
    /// Communication message types.
    /// </summary>
    public enum CommunicationMessageType
    {
        /// <summary>
        /// Represents informational messages used for communication.
        /// </summary>
        Information,

        /// <summary>
        /// Represents a message type indicating a question in the communication.
        /// </summary>
        Question,

        /// <summary>
        /// Represents a request message for communication.
        /// </summary>
        Request,

        /// <summary>
        /// A message type indicating a response in a communication.
        /// </summary>
        Response,

        /// <summary>
        /// Represents an alert or notification within a system.
        /// </summary>
        Alert,

        /// <summary>
        /// Represents a coordination process or mechanism.
        /// </summary>
        Coordination
    }

    /// <summary>
    /// Defines the priority levels for communication handling.
    /// </summary>
    public enum CommunicationPriority
    {
        /// <summary>
        /// Represents the standard level of communication urgency.
        /// </summary>
        Normal
    }

    /// <summary>
    /// Represents the type of recommendation provided.
    /// </summary>
    public enum RecommendationType
    {
        /// <summary>
        /// Recommendation to combine agents for optimal collaboration.
        /// </summary>
        AgentCombination,

        /// <summary>
        /// Recommendation type focused on performance evaluation or improvement.
        /// </summary>
        Performance
    }

    /// <summary>
    /// Represents the priority levels assigned to recommendations.
    /// </summary>
    public enum RecommendationPriority
    {
        /// <summary>
        /// Represents the lowest priority level for a recommendation.
        /// </summary>
        Low,

        /// <summary>
        /// Represents a medium priority level for a recommendation.
        /// </summary>
        Medium,

        /// <summary>
        /// Indicates a high priority recommendation.
        /// </summary>
        High,

        /// <summary>
        /// Represents a critical level of recommendation priority.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Represents the type of request that an agent can make.
    /// </summary>
    public enum AgentRequestType
    {
        /// <summary>
        /// General-purpose agent request type.
        /// </summary>
        General,

        /// <summary>
        /// Request to perform a code review.
        /// </summary>
        CodeReview,

        /// <summary>
        /// A request type indicating a bug fix task.
        /// </summary>
        BugFix,

        /// <summary>
        /// Represents a feature implementation request by the agent.
        /// </summary>
        FeatureImplementation,

        /// <summary>
        /// Request type for creating a new test.
        /// </summary>
        TestCreation,

        /// <summary>
        /// Represents a request type for performing an analysis.
        /// </summary>
        Analysis,

        /// <summary>
        /// Represents the types of requests an agent can make.
        /// </summary>
        Documentation,

        /// <summary>
        /// Request for planning and creating architectural designs.
        /// </summary>
        ArchitectureDesign,

        /// <summary>
        /// Request for collaboration or cooperative engagement.
        /// </summary>
        Collaboration,

        /// <summary>
        /// Request related to communication needs or improvements.
        /// </summary>
        Communication,

        /// <summary>
        /// Request for providing or retrieving the current status update.
        /// </summary>
        StatusUpdate
    }
} 
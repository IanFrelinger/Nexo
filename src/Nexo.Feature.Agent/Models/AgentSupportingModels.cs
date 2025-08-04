using System;
using System.Collections.Generic;
using Nexo.Feature.Agent.Interfaces;

namespace Nexo.Feature.Agent.Models
{
    /// <summary>
    /// Agent candidate for collaboration.
    /// </summary>
    public class AgentCandidate
    {
        public IAIEnhancedAgent Agent { get; set; } = null;
        public double Score { get; set; }
    }

    /// <summary>
    /// Agent capability profile.
    /// </summary>
    public class AgentCapabilityProfile
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string AgentRole { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public List<string> FocusAreas { get; set; } = new List<string>();
        public AIAgentCapabilities AICapabilities { get; set; } = new AIAgentCapabilities();
    }

    /// <summary>
    /// Agent communication context.
    /// </summary>
    public class AgentCommunicationContext
    {
        public string CommunicationId { get; set; } = string.Empty;
        public IAIEnhancedAgent SenderAgent { get; set; } = null;
        public IAIEnhancedAgent RecipientAgent { get; set; } = null;
        public string Message { get; set; } = string.Empty;
        public CommunicationMessageType MessageType { get; set; }
        public CommunicationPriority Priority { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Agent collaboration pattern.
    /// </summary>
    public class AgentCollaborationPattern
    {
        public string AgentId { get; set; } = string.Empty;
        public int CollaborationCount { get; set; }
        public double CollaborationFrequency { get; set; }
    }

    /// <summary>
    /// Session performance metrics.
    /// </summary>
    public class SessionPerformanceMetrics
    {
        public double AverageSessionDuration { get; set; }
        public double AverageAgentsPerSession { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Collaboration recommendation.
    /// </summary>
    public class CollaborationRecommendation
    {
        public RecommendationType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public RecommendationPriority Priority { get; set; }
        public string EstimatedImpact { get; set; } = string.Empty;
    }



    /// <summary>
    /// Communication message types.
    /// </summary>
    public enum CommunicationMessageType
    {
        Information,
        Question,
        Request,
        Response,
        Alert,
        Coordination
    }

    /// <summary>
    /// Communication priority levels.
    /// </summary>
    public enum CommunicationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Recommendation types.
    /// </summary>
    public enum RecommendationType
    {
        AgentCombination,
        Performance,
        Workflow,
        Communication
    }

    /// <summary>
    /// Recommendation priority levels.
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Agent request types.
    /// </summary>
    public enum AgentRequestType
    {
        General,
        CodeReview,
        BugFix,
        FeatureImplementation,
        TestCreation,
        Analysis,
        Documentation,
        ArchitectureDesign,
        Collaboration,
        Communication,
        StatusUpdate
    }
} 
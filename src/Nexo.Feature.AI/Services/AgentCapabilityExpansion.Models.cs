using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Interface and data models for agent capability expansion
    /// </summary>
    public partial class AgentCapabilityExpansion
    {
        // Interface and data models are defined below
    }

    /// <summary>
    /// Interface for agent capability expansion
    /// </summary>
    public interface IAgentCapabilityExpansion
    {
        Task<ExpansionResult> ExpandAgentCapabilitiesAsync(AgentExpansionRequest request);
        Task<CapabilityAssessment> AssessExpansionFeasibilityAsync(AgentExpansionRequest request);
        Task<ExpansionProgress> TrackExpansionProgressAsync(string agentId, string expansionId);
        Task<ExpansionResult> RollbackExpansionAsync(string agentId, string expansionId);
        Task<LearningResult> LearnFromUserFeedbackAsync(string agentId, UserFeedback feedback);
        Task<LearningResult> LearnFromPerformanceMetricsAsync(string agentId, PerformanceMetrics metrics);
        Task<LearningResult> LearnFromExpansionExperienceAsync(AgentExpansionRequest request, ExpansionResult result);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Domain.Entities;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Interfaces
{
    /// <summary>
    /// Defines the contract for an AI-enhanced agent that can leverage AI capabilities
    /// for enhanced task processing and decision-making.
    /// </summary>
    public interface IAiEnhancedAgent : IAgent
    {
        /// <summary>
        /// Gets the AI model orchestrator used by this agent.
        /// </summary>
        IModelOrchestrator ModelOrchestrator { get; }

        /// <summary>
        /// Gets the AI capabilities of this agent.
        /// </summary>
        AiAgentCapabilities AiCapabilities { get; }

        /// <summary>
        /// Processes a request using AI-enhanced capabilities.
        /// </summary>
        /// <param name="request">The AI-enhanced agent request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>AI-enhanced agent response.</returns>
        Task<AiEnhancedAgentResponse> ProcessAiRequestAsync(AiEnhancedAgentRequest request, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Analyzes a task using AI to determine the best approach.
        /// </summary>
        /// <param name="task">The task to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>AI analysis result.</returns>
        Task<AiTaskAnalysisResult> AnalyzeTaskWithAiAsync(SprintTask task, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Generates AI-powered suggestions for task improvement.
        /// </summary>
        /// <param name="task">The task to generate suggestions for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>AI suggestions result.</returns>
        Task<AiSuggestionsResult> GenerateSuggestionsAsync(SprintTask task, CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// AI capabilities for an agent.
    /// </summary>
    public class AiAgentCapabilities
    {
        /// <summary>
        /// Gets or sets whether the agent can perform code analysis.
        /// </summary>
        public bool CanAnalyzeCode { get; set; }

        /// <summary>
        /// Gets or sets whether the agent can generate code.
        /// </summary>
        public bool CanGenerateCode { get; set; }

        /// <summary>
        /// Gets or sets whether the agent can perform task analysis.
        /// </summary>
        public bool CanAnalyzeTasks { get; set; }

        /// <summary>
        /// Gets or sets whether the agent can provide suggestions.
        /// </summary>
        public bool CanProvideSuggestions { get; set; }

        /// <summary>
        /// Gets or sets whether the agent can solve problems.
        /// </summary>
        public bool CanSolveProblems { get; set; }

        /// <summary>
        /// Gets or sets the preferred AI model for this agent.
        /// </summary>
        public string PreferredModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the AI processing strategy.
        /// </summary>
        public AiProcessingStrategy ProcessingStrategy { get; set; } = AiProcessingStrategy.Standard;
    }

    /// <summary>
    /// AI processing strategy for agents.
    /// </summary>
    public enum AiProcessingStrategy
    {
        /// <summary>
        /// Standard processing with basic AI capabilities.
        /// </summary>
        Standard,

        /// <summary>
        /// Advanced processing with enhanced AI capabilities.
        /// </summary>
        Advanced,

        /// <summary>
        /// Conservative processing with minimal AI usage.
        /// </summary>
        Conservative
    }

    /// <summary>
    /// AI-enhanced agent request.
    /// </summary>
    public class AiEnhancedAgentRequest : AgentRequest
    {
        /// <summary>
        /// Gets or sets whether to use AI processing.
        /// </summary>
        public bool UseAi { get; set; } = true;

        /// <summary>
        /// Gets or sets the AI processing strategy.
        /// </summary>
        public AiProcessingStrategy ProcessingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the preferred AI model.
        /// </summary>
        public string PreferredModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional AI context.
        /// </summary>
        public Dictionary<string, object> AiContext { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// AI-enhanced agent response.
    /// </summary>
    public class AiEnhancedAgentResponse : AgentResponse
    {
        /// <summary>
        /// Gets or sets whether AI was used in processing.
        /// </summary>
        public bool AiWasUsed { get; set; }

        /// <summary>
        /// Gets or sets the AI model used.
        /// </summary>
        public string AiModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the AI processing time in milliseconds.
        /// </summary>
        public long AiProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets AI-generated insights.
        /// </summary>
        public List<string> AiInsights { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets AI confidence score.
        /// </summary>
        public double AiConfidenceScore { get; set; }
    }

    /// <summary>
    /// AI task analysis result.
    /// </summary>
    public class AiTaskAnalysisResult
    {
        /// <summary>
        /// Gets or sets the analysis summary.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the complexity assessment.
        /// </summary>
        public string ComplexityAssessment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated effort.
        /// </summary>
        public string EstimatedEffort { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recommended approach.
        /// </summary>
        public string RecommendedApproach { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the potential risks.
        /// </summary>
        public List<string> PotentialRisks { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the confidence score.
        /// </summary>
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// AI suggestions result.
    /// </summary>
    public class AiSuggestionsResult
    {
        /// <summary>
        /// Gets or sets the improvement suggestions.
        /// </summary>
        public List<string> ImprovementSuggestions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the code suggestions.
        /// </summary>
        public List<string> CodeSuggestions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the architectural suggestions.
        /// </summary>
        public List<string> ArchitecturalSuggestions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the testing suggestions.
        /// </summary>
        public List<string> TestingSuggestions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the confidence score.
        /// </summary>
        public double ConfidenceScore { get; set; }
    }
} 
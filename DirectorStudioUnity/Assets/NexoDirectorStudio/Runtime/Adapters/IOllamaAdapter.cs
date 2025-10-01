using NexoDirectorStudio.Validators;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Interface for Ollama LLM adapter.
    /// Provides offline LLM capabilities for game plan generation and analysis.
    /// </summary>
    public interface IOllamaAdapter : IAdapter
    {
        /// <summary>
        /// Generates a game plan from a design brief using the LLM.
        /// </summary>
        /// <param name="brief">The design brief to process</param>
        /// <param name="seed">Random seed for deterministic generation</param>
        /// <param name="genreId">Genre ID for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated game plan</returns>
        Task<GamePlan> PlanAsync(DesignBrief brief, int seed, string genreId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Analyzes a game plan and provides suggestions.
        /// </summary>
        /// <param name="gamePlan">The game plan to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analysis result with suggestions</returns>
        Task<GamePlanAnalysis> AnalyzeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates auto-fix suggestions for validation issues.
        /// </summary>
        /// <param name="validationReport">The validation report to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Auto-fix suggestions</returns>
        Task<AutoFixSuggestions> SuggestFixesAsync(ValidationReport validationReport, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates narrative content for the game plan.
        /// </summary>
        /// <param name="gamePlan">The game plan to enhance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Enhanced game plan with narrative content</returns>
        Task<GamePlan> EnhanceNarrativeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Result of game plan analysis.
    /// </summary>
    public sealed record GamePlanAnalysis
    {
        /// <summary>
        /// Overall quality score (0-100).
        /// </summary>
        public int QualityScore { get; init; }
        
        /// <summary>
        /// Analysis summary.
        /// </summary>
        public string Summary { get; init; } = "";
        
        /// <summary>
        /// Detailed analysis.
        /// </summary>
        public string Details { get; init; } = "";
        
        /// <summary>
        /// List of suggestions for improvement.
        /// </summary>
        public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Potential issues identified.
        /// </summary>
        public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Strengths of the game plan.
        /// </summary>
        public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
    }
    
    /// <summary>
    /// Auto-fix suggestions from LLM analysis.
    /// </summary>
    public sealed record AutoFixSuggestions
    {
        /// <summary>
        /// List of suggested fixes.
        /// </summary>
        public IReadOnlyList<AutoFixSuggestion> Suggestions { get; init; } = Array.Empty<AutoFixSuggestion>();
        
        /// <summary>
        /// Overall confidence in the suggestions (0-100).
        /// </summary>
        public int ConfidenceScore { get; init; }
        
        /// <summary>
        /// Summary of the suggestions.
        /// </summary>
        public string Summary { get; init; } = "";
    }
    
    /// <summary>
    /// Individual auto-fix suggestion.
    /// </summary>
    public sealed record AutoFixSuggestion
    {
        /// <summary>
        /// Title of the suggestion.
        /// </summary>
        public string Title { get; init; } = "";
        
        /// <summary>
        /// Description of the suggestion.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// Priority of the suggestion (1-5).
        /// </summary>
        public int Priority { get; init; }
        
        /// <summary>
        /// Effort required to implement (Low/Medium/High).
        /// </summary>
        public string Effort { get; init; } = "Medium";
        
        /// <summary>
        /// Specific changes to make.
        /// </summary>
        public string Changes { get; init; } = "";
        
        /// <summary>
        /// Confidence in this suggestion (0-100).
        /// </summary>
        public int Confidence { get; init; }
    }
}

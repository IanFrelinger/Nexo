using System;
using System.Collections.Generic;
using System.Linq;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Information about an Ollama model.
    /// </summary>
    public sealed record ModelInfo
    {
        public string Name { get; init; } = "";
        public string Size { get; init; } = "";
        public string ModifiedAt { get; init; } = "";
    }

    /// <summary>
    /// Response from Ollama API.
    /// </summary>
    public sealed record OllamaResponse
    {
        public string Response { get; init; } = "";
        public bool Done { get; init; }
        public string Model { get; init; } = "";
    }

    /// <summary>
    /// Configuration for Ollama adapter.
    /// </summary>
    public sealed record OllamaConfig
    {
        public string BaseUrl { get; init; } = "http://localhost:11434";
        public string Model { get; init; } = "llama2";
        public int TimeoutSeconds { get; init; } = 30;
        public int MaxRetries { get; init; } = 3;
    }

    /// <summary>
    /// Interface for Ollama prompt generators.
    /// </summary>
    public interface IOllamaPromptGenerator
    {
        string GeneratePlanningPrompt(DesignBrief brief, string genreId);
        string GenerateAnalysisPrompt(GamePlan gamePlan);
        string GenerateFixSuggestionPrompt(ValidationReport validationReport);
        string GenerateNarrativeEnhancementPrompt(GamePlan gamePlan);
    }

    /// <summary>
    /// Interface for Ollama response parsers.
    /// </summary>
    public interface IOllamaResponseParser
    {
        GamePlan ParseGamePlanResponse(string response, DesignBrief brief, int seed);
        GamePlanAnalysis ParseAnalysisResponse(string response);
        AutoFixSuggestions ParseFixSuggestionResponse(string response);
        GamePlan ParseNarrativeEnhancementResponse(string response, GamePlan originalPlan);
    }

    /// <summary>
    /// Interface for Ollama HTTP client.
    /// </summary>
    public interface IOllamaHttpClient
    {
        Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default);
        Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
        void Dispose();
    }
}

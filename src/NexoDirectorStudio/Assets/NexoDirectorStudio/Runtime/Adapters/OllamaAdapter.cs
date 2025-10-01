using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Implementation of IOllamaAdapter for offline LLM integration.
    /// Provides game plan generation and analysis using Ollama.
    /// </summary>
    public sealed class OllamaAdapter : IOllamaAdapter, IDisposable
    {
        private readonly IOllamaHttpClient _httpClient;
        private readonly IOllamaPromptGenerator _promptGenerator;
        private readonly IOllamaResponseParser _responseParser;
        private readonly string _model;
        private bool _disposed;
        
        public string Id => "ollama";
        public string Name => "Ollama LLM Adapter";
        public string Description => "Offline LLM adapter using Ollama for game plan generation and analysis";
        public bool IsAvailable { get; private set; }
        public DateTime LastHealthCheck { get; private set; }
        
        public OllamaAdapter(string baseUrl = "http://localhost:11434", string model = "llama2")
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _httpClient = new OllamaHttpClient(baseUrl, model);
            _promptGenerator = new OllamaPromptGenerator();
            _responseParser = new OllamaResponseParser();
        }
        
        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.HealthCheckAsync(cancellationToken);
            IsAvailable = result.IsHealthy;
            LastHealthCheck = result.Timestamp;
            return result;
        }
        
        public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.InitializeAsync(cancellationToken);
            IsAvailable = result.Success;
            return result;
        }
        
        public async Task<GamePlan> PlanAsync(DesignBrief brief, int seed, string genreId, CancellationToken cancellationToken = default)
        {
            if (brief == null)
                throw new ArgumentNullException(nameof(brief));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available. Please check health status.");
            }

            var prompt = _promptGenerator.GeneratePlanningPrompt(brief, genreId);
            var response = await _httpClient.GenerateResponseAsync(prompt, cancellationToken);
            return _responseParser.ParseGamePlanResponse(response, brief, seed);
        }
        
        public async Task<GamePlanAnalysis> AnalyzeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default)
        {
            if (gamePlan == null)
                throw new ArgumentNullException(nameof(gamePlan));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available. Please check health status.");
            }

            var prompt = _promptGenerator.GenerateAnalysisPrompt(gamePlan);
            var response = await _httpClient.GenerateResponseAsync(prompt, cancellationToken);
            return _responseParser.ParseAnalysisResponse(response);
        }
        
        public async Task<AutoFixSuggestions> SuggestFixesAsync(ValidationReport validationReport, CancellationToken cancellationToken = default)
        {
            if (validationReport == null)
                throw new ArgumentNullException(nameof(validationReport));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available. Please check health status.");
            }

            var prompt = _promptGenerator.GenerateFixSuggestionPrompt(validationReport);
            var response = await _httpClient.GenerateResponseAsync(prompt, cancellationToken);
            return _responseParser.ParseFixSuggestionResponse(response);
        }
        
        public async Task<GamePlan> EnhanceNarrativeAsync(GamePlan gamePlan, CancellationToken cancellationToken = default)
        {
            if (gamePlan == null)
                throw new ArgumentNullException(nameof(gamePlan));
            
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Ollama adapter is not available. Please check health status.");
            }

            var prompt = _promptGenerator.GenerateNarrativeEnhancementPrompt(gamePlan);
            var response = await _httpClient.GenerateResponseAsync(prompt, cancellationToken);
            return _responseParser.ParseNarrativeEnhancementResponse(response, gamePlan);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}

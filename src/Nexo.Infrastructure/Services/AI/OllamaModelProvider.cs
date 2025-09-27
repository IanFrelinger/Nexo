using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using IModelProvider = Nexo.Feature.AI.Interfaces.IModelProvider;
using ModelInfo = Nexo.Feature.AI.Models.ModelInfo;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Ollama model provider implementation.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class OllamaModelProvider : IModelProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaModelProvider> _logger;

        public OllamaModelProvider(
            HttpClient httpClient,
            ILogger<OllamaModelProvider> logger,
            string baseUrl = "http://localhost:11434")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-AI-Provider/1.0");
        }

        // IModelProvider interface implementation
        public string ProviderId => "ollama";
        public string DisplayName => "Ollama";
        public string Description => "Local Ollama models for text generation and chat";
        public bool IsAvailable => true; // Assume available if we can reach the service

        public IEnumerable<ModelType> SupportedModelTypes =>
        [
            ModelType.TextGeneration,
            ModelType.CodeGeneration
        ];

        // Legacy methods for backward compatibility
        public string Name => DisplayName;
        public string ProviderType => "Ollama";
        public bool IsEnabled => true;
        public bool IsPrimary => false;

        public ModelCapabilities Capabilities => new ModelCapabilities
        {
            SupportsTextGeneration = true,
            SupportsCodeGeneration = true,
            SupportsAnalysis = true,
            SupportsOptimization = false,
            SupportsStreaming = true
        };
        // This class acts as an orchestrator for various Ollama model provider functionalities,
        // with specific categories defined in partial classes.
    }
}
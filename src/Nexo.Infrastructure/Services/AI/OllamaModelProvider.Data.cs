using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Data models and DTOs for Ollama API
    /// </summary>
    public partial class OllamaModelProvider
    {
        // Data models are defined here for Ollama API communication
    }

    /// <summary>
    /// Ollama API request model
    /// </summary>
    public class OllamaRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public bool Stream { get; set; } = false;
        public OllamaOptions Options { get; set; } = new();
    }

    /// <summary>
    /// Ollama API response model
    /// </summary>
    public class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
        public string Model { get; set; } = string.Empty;
        public OllamaUsage Usage { get; set; } = new();
    }

    /// <summary>
    /// Ollama models response
    /// </summary>
    public class OllamaModelsResponse
    {
        public List<OllamaModelInfo> Models { get; set; } = new();
    }

    /// <summary>
    /// Ollama model information
    /// </summary>
    public class OllamaModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Digest { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
    }

    /// <summary>
    /// Ollama options
    /// </summary>
    public class OllamaOptions
    {
        public float Temperature { get; set; } = 0.7f;
        public float TopP { get; set; } = 0.9f;
        public int MaxTokens { get; set; } = 1000;
        public int TopK { get; set; } = 40;
        public float RepeatPenalty { get; set; } = 1.1f;
    }

    /// <summary>
    /// Ollama usage information
    /// </summary>
    public class OllamaUsage
    {
        public int PromptEvalCount { get; set; }
        public long PromptEvalDuration { get; set; }
        public int EvalCount { get; set; }
        public long EvalDuration { get; set; }
    }
}

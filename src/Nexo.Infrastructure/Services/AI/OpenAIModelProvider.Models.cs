using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using static Nexo.Feature.AI.Enums.ModelType;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// OpenAI model implementation
/// </summary>
public class OpenAiModel : IModel
{
    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private ModelInfo? _info;

    public OpenAiModel(string modelName, HttpClient httpClient, ILogger logger)
    {
        _modelName = modelName;
        _httpClient = httpClient;
        _logger = logger;
    }

    public ModelInfo Info
    {
        get
        {
            return _info ??= new ModelInfo
            {
                Name = _modelName,
                DisplayName = _modelName,
                ModelType = Nexo.Feature.AI.Enums.ModelType.TextGeneration,
                IsAvailable = true,
                SizeBytes = 1024 * 1024 * 1024,
                MaxContextLength = 4096,
                Capabilities = new ModelCapabilities
                {
                    SupportsTextGeneration = true,
                    SupportsCodeGeneration = true,
                    SupportsAnalysis = true,
                    SupportsOptimization = false,
                    SupportsStreaming = true
                }
            };
        }
    }

    public bool IsLoaded => true;

    /// <summary>
    /// Unique identifier for this model instance
    /// </summary>
    public string ModelId => _modelName;

    /// <summary>
    /// Human-readable name of the model
    /// </summary>
    public string Name => _modelName;

    /// <summary>
    /// Type of model
    /// </summary>
    public Nexo.Feature.AI.Enums.ModelType ModelType => Nexo.Feature.AI.Enums.ModelType.TextGeneration;

    public async Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        // Create a new provider instance with a null logger to avoid type issues
        var provider = new OpenAiModelProvider(_httpClient, Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAiModelProvider>.Instance, "dummy-key");
        return await provider.ExecuteAsync(request, cancellationToken);
    }

    public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException("Streaming not implemented yet");
    }

    public ModelCapabilities GetCapabilities()
    {
        return new ModelCapabilities
        {
            SupportsTextGeneration = true,
            SupportsCodeGeneration = true,
            SupportsAnalysis = true,
            SupportsOptimization = false,
            SupportsStreaming = true
        };
    }

    public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // OpenAI models are loaded on demand
        return Task.CompletedTask;
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // No cleanup needed for OpenAI models
        return Task.CompletedTask;
    }
}

/// <summary>
/// OpenAI response models
/// </summary>
public class OpenAiResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public string Model { get; set; }
    public List<OpenAiChoice> Choices { get; set; }
    public OpenAiUsage Usage { get; set; }
    public OpenAiResponse()
    {
        Id = string.Empty;
        Object = string.Empty;
        Created = 0;
        Model = string.Empty;
        Choices = new List<OpenAiChoice>();
        Usage = new OpenAiUsage();
    }
}

public class OpenAiChoice
{
    public int Index { get; set; }
    public OpenAiMessage Message { get; set; }
    public string FinishReason { get; set; }
    public OpenAiChoice()
    {
        Index = 0;
        Message = new OpenAiMessage();
        FinishReason = string.Empty;
    }
}

public class OpenAiMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public OpenAiMessage()
    {
        Role = string.Empty;
        Content = string.Empty;
    }
}

public class OpenAiUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public OpenAiUsage()
    {
        PromptTokens = 0;
        CompletionTokens = 0;
        TotalTokens = 0;
    }
}

public class OpenAiModelsResponse
{
    public List<OpenAiModelData> Data { get; set; }
    public OpenAiModelsResponse()
    {
        Data = [];
    }
}

public class OpenAiModelData
{
    public string Id { get; set; }
    public string OwnedBy { get; set; }
    public List<object> Permission { get; set; }
    public OpenAiModelData()
    {
        Id = string.Empty;
        OwnedBy = string.Empty;
        Permission = new List<object>();
    }
}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Mock model provider for testing and development
/// </summary>
public class MockModelProvider : IModelProvider
{
    private readonly ILogger<MockModelProvider> _logger;
    private readonly List<ModelInfo> _availableModels;

    public MockModelProvider(ILogger<MockModelProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _availableModels = new List<ModelInfo>
        {
            new()
            {
                Name = "mock-text-model",
                DisplayName = "Mock Text Generation Model",
                ModelType = Enums.ModelType.TextGeneration,
                IsAvailable = true,
                SizeBytes = 1024 * 1024, // 1MB
                MaxContextLength = 4000,
                Capabilities = new ModelCapabilities
                {
                    SupportsTextGeneration = true,
                    SupportsCodeGeneration = true,
                    SupportsAnalysis = false,
                    SupportsOptimization = false,
                    SupportsStreaming = false
                }
            },
            new()
            {
                Name = "mock-code-model",
                DisplayName = "Mock Code Generation Model",
                ModelType = Enums.ModelType.CodeGeneration,
                IsAvailable = true,
                SizeBytes = 2 * 1024 * 1024, // 2MB
                MaxContextLength = 8000,
                Capabilities = new ModelCapabilities
                {
                    SupportsTextGeneration = false,
                    SupportsCodeGeneration = true,
                    SupportsAnalysis = true,
                    SupportsOptimization = true,
                    SupportsStreaming = false
                }
            }
        };
    }

    public string ProviderId => "mock-provider";
    public string DisplayName => "Mock Model Provider";
    public string Name => "MockProvider";
    public IEnumerable<Enums.ModelType> SupportedModelTypes => new[] 
    { 
        Enums.ModelType.TextGeneration, 
        Enums.ModelType.CodeGeneration 
    };

    public Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Returning {Count} available models", _availableModels.Count);
        return Task.FromResult<IEnumerable<ModelInfo>>(_availableModels);
    }

    public Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        var modelInfo = _availableModels.FirstOrDefault(m => m.Name == modelName);
        if (modelInfo == null)
        {
            throw new ArgumentException($"Model '{modelName}' not found", nameof(modelName));
        }

        var model = new MockModel(modelInfo, _logger);
        _logger.LogInformation("Loaded model: {ModelName}", modelName);
        
        return Task.FromResult<IModel>(model);
    }

    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing request with mock provider");
        
        // Simulate processing time
        await Task.Delay(100, cancellationToken);
        
        // Generate a mock response based on the input
        var response = GenerateMockResponse(request);
        
        _logger.LogDebug("Generated mock response with {Length} characters", response.Response.Length);
        return response;
    }

    public Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new ModelHealthStatus
        {
            IsHealthy = true,
            Status = "Mock provider is healthy",
            LastChecked = DateTime.UtcNow,
            ResponseTimeMs = 50,
            ErrorRate = 0.0,
            Metrics = new Dictionary<string, object>
            {
                ["mock_metric"] = "mock_value",
                ["uptime_hours"] = 24.5
            }
        };

        return Task.FromResult(status);
    }

    private ModelResponse GenerateMockResponse(ModelRequest request)
    {
        var input = request.Input.ToLowerInvariant();
        
        // Generate different responses based on input content
        string response;
        if (input.Contains("iteration") || input.Contains("loop") || input.Contains("foreach"))
        {
            response = GenerateIterationCodeResponse(request);
        }
        else if (input.Contains("code") || input.Contains("generate"))
        {
            response = GenerateCodeResponse(request);
        }
        else
        {
            response = GenerateGenericResponse(request);
        }

        return new ModelResponse
        {
            Response = response,
            Success = true,
            InputTokens = EstimateTokenCount(request.Input),
            OutputTokens = EstimateTokenCount(response),
            ProcessingTimeMs = 100,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "mock",
                ["model"] = "mock-model",
                ["generated_at"] = DateTime.UtcNow
            }
        };
    }

    private string GenerateIterationCodeResponse(ModelRequest request)
    {
        return """
            // Optimized iteration code generated by AI
            foreach (var item in items)
            {
                if (item != null)
                {
                    // Process the item
                    ProcessItem(item);
                }
            }
            
            // Alternative: LINQ approach for better readability
            items.Where(item => item != null)
                 .ToList()
                 .ForEach(ProcessItem);
            """;
    }

    private string GenerateCodeResponse(ModelRequest request)
    {
        return """
            // AI-generated code with best practices
            public class OptimizedProcessor
            {
                public void ProcessItems<T>(IEnumerable<T> items, Action<T> processor)
                {
                    ArgumentNullException.ThrowIfNull(items);
                    ArgumentNullException.ThrowIfNull(processor);
                    
                    foreach (var item in items)
                    {
                        processor(item);
                    }
                }
            }
            """;
    }

    private string GenerateGenericResponse(ModelRequest request)
    {
        return $"Mock response for: {request.Input.Substring(0, Math.Min(50, request.Input.Length))}...";
    }

    private int EstimateTokenCount(string text)
    {
        // Rough estimation: 1 token ≈ 4 characters
        return Math.Max(1, text.Length / 4);
    }
}

/// <summary>
/// Mock model implementation
/// </summary>
public class MockModel : IModel
{
    private readonly ModelInfo _modelInfo;
    private readonly ILogger _logger;
    private bool _isLoaded = false;

    public MockModel(ModelInfo modelInfo, ILogger logger)
    {
        _modelInfo = modelInfo ?? throw new ArgumentNullException(nameof(modelInfo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ModelId => _modelInfo.Name;
    public string Name => _modelInfo.DisplayName;
    public Enums.ModelType ModelType => _modelInfo.ModelType;
    public bool IsLoaded => _isLoaded;

    public Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        if (!_isLoaded)
        {
            throw new InvalidOperationException("Model is not loaded");
        }

        _logger.LogDebug("Processing request with model: {ModelName}", Name);
        
        // Simple mock processing
        var response = new ModelResponse
        {
            Response = $"Mock response from {Name}: {request.Input}",
            Success = true,
            InputTokens = EstimateTokenCount(request.Input),
            OutputTokens = 10,
            ProcessingTimeMs = 50
        };

        return Task.FromResult(response);
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading model: {ModelName}", Name);
        _isLoaded = true;
        return Task.CompletedTask;
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unloading model: {ModelName}", Name);
        _isLoaded = false;
        return Task.CompletedTask;
    }

    private int EstimateTokenCount(string text)
    {
        return Math.Max(1, text.Length / 4);
    }
}

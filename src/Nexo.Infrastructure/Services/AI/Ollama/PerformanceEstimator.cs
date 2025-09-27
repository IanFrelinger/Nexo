using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.Ollama;

/// <summary>
/// Provides performance estimation for Ollama operations.
/// </summary>
public partial class PerformanceEstimator
{
    public async Task<PerformanceEstimate> EstimatePerformanceAsync(AIOperationContext context)
    {
        await Task.CompletedTask;
        return new PerformanceEstimate
        {
            EstimatedDuration = TimeSpan.FromSeconds(3),
            EstimatedMemoryUsage = 512 * 1024 * 1024, // 512MB
            EstimatedCpuUsage = 0.6,
            Confidence = 0.8
        };
    }

    public static ModelType GetModelType(string modelId)
    {
        if (modelId.Contains("code") || modelId.Contains("codellama"))
        {
            return ModelType.CodeGeneration;
        }
        if (modelId.Contains("chat"))
        {
            return ModelType.Chat;
        }
        return ModelType.TextGeneration;
    }

    public static bool IsModelSupported(string model)
    {
        var supportedModels = new[] { "llama2", "llama2:7b", "llama2:13b", "llama2:70b", "codellama", "codellama:7b", "codellama:13b", "mistral", "mistral:7b" };
        return supportedModels.Contains(model.ToLower());
    }
}

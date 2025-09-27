using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.Ollama;

/// <summary>
/// Provides Ollama capabilities and compatibility information.
/// </summary>
public partial class CapabilityProvider
{
    public string ProviderId => "ollama";
    public string DisplayName => "Ollama";
    public string Description => "Local Ollama models for offline AI operations";
    public int Priority => 95; // Higher priority than remote providers
    public bool IsOfflineCapable => true;
    public bool SupportsGpuAcceleration => true; // Ollama supports GPU acceleration
    public bool SupportsStreaming => true;
    public int MaxContextLength => 8192; // Typical for Ollama models
    public string ModelsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "models", "ollama");

    public IEnumerable<string> SupportedModelTypes => new[]
    {
        "TextGeneration",
        "CodeGeneration",
        "Chat"
    };

    public AIProviderType ProviderType => AIProviderType.Ollama;
    public string Name => "Ollama Provider";
    public string Version => "1.0.0";

    public bool IsAvailable() => true; // Ollama is always available if running

    public AIProviderCapabilities Capabilities => new AIProviderCapabilities
    {
        ProviderType = AIProviderType.Ollama,
        SupportedPlatforms = new List<PlatformType> { PlatformType.Windows, PlatformType.Linux, PlatformType.macOS },
        SupportedOperations = new List<AIOperationType> { AIOperationType.CodeGeneration, AIOperationType.CodeReview, AIOperationType.CodeOptimization },
        SupportsOfflineMode = true,
        SupportsStreaming = true,
        SupportsBatchProcessing = false,
        MaxConcurrentOperations = 1
    };

    public AIProviderStatus Status => AIProviderStatus.Available;

    public bool SupportsPlatform(PlatformType platform)
    {
        return platform == PlatformType.Windows || platform == PlatformType.Linux || platform == PlatformType.macOS;
    }

    public bool MeetsRequirements(AIRequirements requirements)
    {
        return requirements.RequiresOfflineMode && IsOfflineCapable;
    }

    public bool HasRequiredResources(AIResources resources)
    {
        return true; // Ollama manages its own resources
    }

    public bool IsModelCompatible(ModelInfo model)
    {
        return model.ModelType == "Llama" || model.ModelType == "TextGeneration";
    }

    public bool SupportsEngineType(AIEngineType engineType)
    {
        return engineType == AIEngineType.Llama;
    }

    public AIEngineType EngineType => AIEngineType.Llama;
    public AIProviderType Provider => AIProviderType.Ollama;
}

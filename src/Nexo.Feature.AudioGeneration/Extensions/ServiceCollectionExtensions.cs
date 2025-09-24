using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Feature.AudioGeneration.Interfaces;
using Nexo.Feature.AudioGeneration.Providers;
using Nexo.Feature.AudioGeneration.Services;
using Nexo.Feature.AudioGeneration.Tools;

namespace Nexo.Feature.AudioGeneration.Extensions;

/// <summary>
/// Extension methods for registering audio generation services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds audio generation services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddAudioGenerationServices(this IServiceCollection services)
    {
        // Register providers
        services.AddSingleton<IAudioGenerationProvider, OllamaAudioGenerationProvider>();
        services.AddSingleton<IAudioGenerationProvider, OpenAIAudioGenerationProvider>();
        
        // Register orchestrator
        services.AddSingleton<AudioGenerationOrchestrator>();
        
        // Register agent tool
        services.AddTransient<ITool, AudioGenerationTool>();
        
        return services;
    }

    /// <summary>
    /// Adds offline-only audio generation services
    /// </summary>
    public static IServiceCollection AddOfflineAudioGeneration(this IServiceCollection services)
    {
        // Register only offline providers
        services.AddSingleton<IAudioGenerationProvider, OllamaAudioGenerationProvider>();
        
        // Register orchestrator
        services.AddSingleton<AudioGenerationOrchestrator>();
        
        // Register agent tool
        services.AddTransient<ITool, AudioGenerationTool>();
        
        return services;
    }

    /// <summary>
    /// Adds online-only audio generation services
    /// </summary>
    public static IServiceCollection AddOnlineAudioGeneration(this IServiceCollection services)
    {
        // Register only online providers
        services.AddSingleton<IAudioGenerationProvider, OpenAIAudioGenerationProvider>();
        
        // Register orchestrator
        services.AddSingleton<AudioGenerationOrchestrator>();
        
        // Register agent tool
        services.AddTransient<ITool, AudioGenerationTool>();
        
        return services;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.ImageGeneration.Interfaces;
using Nexo.Feature.ImageGeneration.Providers;
using Nexo.Feature.ImageGeneration.Services;
using Nexo.Feature.ImageGeneration.Tools;

namespace Nexo.Feature.ImageGeneration.Extensions;

/// <summary>
/// Extension methods for registering image generation services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds image generation services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddImageGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration
        services.Configure<ImageGenerationConfiguration>(configuration.GetSection("ImageGeneration"));

        // Register providers
        services.AddSingleton<IImageGenerationProvider, OllamaImageGenerationProvider>();
        
        // Register OpenAI provider if API key is available
        var openAiApiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAiApiKey))
        {
            services.AddHttpClient<OpenAIImageGenerationProvider>();
            services.AddSingleton<IImageGenerationProvider>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<OpenAIImageGenerationProvider>>();
                var httpClient = provider.GetRequiredService<HttpClient>();
                return new OpenAIImageGenerationProvider(logger, httpClient, openAiApiKey);
            });
        }

        // Register orchestrator
        services.AddSingleton<ImageGenerationOrchestrator>();

        // Register agent tool
        services.AddSingleton<ImageGenerationTool>();

        return services;
    }

    /// <summary>
    /// Adds image generation services with custom configuration
    /// </summary>
    public static IServiceCollection AddImageGeneration(this IServiceCollection services, Action<ImageGenerationConfiguration> configure)
    {
        var configuration = new ImageGenerationConfiguration();
        configure(configuration);

        // Register configuration
        services.AddSingleton(configuration);

        // Register providers
        services.AddSingleton<IImageGenerationProvider, OllamaImageGenerationProvider>();

        // Register orchestrator
        services.AddSingleton<ImageGenerationOrchestrator>();

        // Register agent tool
        services.AddSingleton<ImageGenerationTool>();

        return services;
    }

    /// <summary>
    /// Adds image generation services with offline-only configuration
    /// </summary>
    public static IServiceCollection AddOfflineImageGeneration(this IServiceCollection services)
    {
        var configuration = new ImageGenerationConfiguration
        {
            PreferOffline = true,
            PreferredProviders = new List<string> { "ollama-image-generation" }
        };

        services.AddSingleton(configuration);
        services.AddSingleton<IImageGenerationProvider, OllamaImageGenerationProvider>();
        services.AddSingleton<ImageGenerationOrchestrator>();
        services.AddSingleton<ImageGenerationTool>();

        return services;
    }

    /// <summary>
    /// Adds image generation services with online-only configuration
    /// </summary>
    public static IServiceCollection AddOnlineImageGeneration(this IServiceCollection services, string openAiApiKey)
    {
        var configuration = new ImageGenerationConfiguration
        {
            PreferOffline = false,
            PreferredProviders = new List<string> { "openai-image-generation" }
        };

        services.AddSingleton(configuration);
        services.AddHttpClient<OpenAIImageGenerationProvider>();
        services.AddSingleton<IImageGenerationProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OpenAIImageGenerationProvider>>();
            var httpClient = provider.GetRequiredService<HttpClient>();
            return new OpenAIImageGenerationProvider(logger, httpClient, openAiApiKey);
        });
        services.AddSingleton<ImageGenerationOrchestrator>();
        services.AddSingleton<ImageGenerationTool>();

        return services;
    }
}

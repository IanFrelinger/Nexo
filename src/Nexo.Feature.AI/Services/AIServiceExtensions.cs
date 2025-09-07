using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Extension methods for registering AI services with dependency injection
/// </summary>
public static class AIServiceExtensions
{
    /// <summary>
    /// Adds AI services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAIServices(this IServiceCollection services)
    {
        // Core AI services
        services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
        services.AddTransient<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator, IterationCodeGenerator>();
        
        // Mock provider for development and testing
        services.AddTransient<IModelProvider, MockModelProvider>();
        
        return services;
    }

    /// <summary>
    /// Adds AI services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure AI options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAIServices(
        this IServiceCollection services, 
        Action<AIServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddAIServices();
    }

    /// <summary>
    /// Adds AI services with iteration strategy integration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAIWithIterationStrategies(this IServiceCollection services)
    {
        // Add iteration strategies first
        services.AddIterationStrategies();
        
        // Add AI services
        services.AddAIServices();
        
        // Register the iteration code generator with the iteration system
        services.AddTransient<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator, IterationCodeGenerator>();
        
        return services;
    }
}

/// <summary>
/// Configuration options for AI services
/// </summary>
public class AIServiceOptions
{
    /// <summary>
    /// Whether to enable AI code generation
    /// </summary>
    public bool EnableCodeGeneration { get; set; } = true;
    
    /// <summary>
    /// Default temperature for AI requests
    /// </summary>
    public double DefaultTemperature { get; set; } = 0.7;
    
    /// <summary>
    /// Default maximum tokens for AI requests
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 1000;
    
    /// <summary>
    /// Whether to use mock providers in development
    /// </summary>
    public bool UseMockProviders { get; set; } = true;
    
    /// <summary>
    /// Timeout for AI requests in milliseconds
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;
}

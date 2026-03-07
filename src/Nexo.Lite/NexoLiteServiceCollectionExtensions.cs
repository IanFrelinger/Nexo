using Microsoft.Extensions.DependencyInjection;
using Nexo.Client;

namespace Nexo.Lite;

/// <summary>
/// Extension methods for registering Nexo Lite (slim host for mobile/edge).
/// Use when running on device without Roslyn, Docker, or code analysis.
/// Orchestration delegates to Nexo API when connected; optional local model for air-gapped.
/// </summary>
public static class NexoLiteServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nexo Lite services: client for API, optional local model when NEXO_LOCAL_MODEL_PATH is set.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiBaseUrl">Base URL of Nexo API (e.g. https://your-server:5000). Required when not air-gapped.</param>
    public static IServiceCollection AddNexoLite(this IServiceCollection services, string apiBaseUrl)
    {
        services.AddNexoClient(apiBaseUrl);
        // Local model (LlamaSharp) requires Nexo.Infrastructure; for true Lite, use thin client only.
        // When NEXO_LOCAL_MODEL_PATH is set and Infra is available, provider=local can be used.
        return services;
    }
}

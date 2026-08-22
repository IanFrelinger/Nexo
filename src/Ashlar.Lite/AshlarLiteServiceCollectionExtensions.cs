using Microsoft.Extensions.DependencyInjection;
using Ashlar.Client;

namespace Ashlar.Lite;

/// <summary>
/// Extension methods for registering Ashlar Lite (slim host for mobile/edge).
/// Use when running on device without Roslyn, Docker, or code analysis.
/// Orchestration delegates to Ashlar API when connected; optional local model for air-gapped.
/// </summary>
public static class AshlarLiteServiceCollectionExtensions
{
    /// <summary>
    /// Adds Ashlar Lite services: client for API, optional local model when ASHLAR_LOCAL_MODEL_PATH is set.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiBaseUrl">Base URL of Ashlar API (e.g. https://your-server:5000). Required when not air-gapped.</param>
    public static IServiceCollection AddAshlarLite(this IServiceCollection services, string apiBaseUrl)
    {
        services.AddAshlarClient(apiBaseUrl);
        // Local model (LlamaSharp) requires Ashlar.Infrastructure; for true Lite, use thin client only.
        // When ASHLAR_LOCAL_MODEL_PATH is set and Infra is available, provider=local can be used.
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nexo.Client;

/// <summary>
/// Extension methods for registering Nexo client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Nexo API client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure NexoClientOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoClient(this IServiceCollection services, Action<NexoClientOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient<INexoClient, NexoClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NexoClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = options.Timeout;
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                client.DefaultRequestHeaders.Add("X-Nexo-Api-Key", options.ApiKey);
        });
        return services;
    }

    /// <summary>
    /// Adds the Nexo API client with the given base URL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">Base URL of the Nexo API host.</param>
    /// <param name="apiKey">Optional API key sent as <c>X-Nexo-Api-Key</c>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoClient(this IServiceCollection services, string baseUrl, string? apiKey = null)
    {
        return services.AddNexoClient(options =>
        {
            options.BaseUrl = baseUrl;
            options.ApiKey = apiKey;
        });
    }
}

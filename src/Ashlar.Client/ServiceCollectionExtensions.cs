using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ashlar.Client;

/// <summary>
/// Extension methods for registering Ashlar client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Ashlar API client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure AshlarClientOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAshlarClient(this IServiceCollection services, Action<AshlarClientOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient<IAshlarClient, AshlarClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AshlarClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = options.Timeout;
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                client.DefaultRequestHeaders.Add("X-Ashlar-Api-Key", options.ApiKey);
        });
        return services;
    }

    /// <summary>
    /// Adds the Ashlar API client with the given base URL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">Base URL of the Ashlar API host.</param>
    /// <param name="apiKey">Optional API key sent as <c>X-Ashlar-Api-Key</c>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAshlarClient(this IServiceCollection services, string baseUrl, string? apiKey = null)
    {
        return services.AddAshlarClient(options =>
        {
            options.BaseUrl = baseUrl;
            options.ApiKey = apiKey;
        });
    }
}

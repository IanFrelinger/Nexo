using Microsoft.Extensions.DependencyInjection;
using Ashlar.Client;

namespace Ashlar.Lite;

/// <summary>
/// Registration helper for the slim edge/mobile host. Runs without Roslyn, Docker, or code
/// analysis, and talks to an Ashlar API server over HTTP.
///
/// <para>This package provides only the client wiring. A local (on-device) model is NOT
/// included here — that requires Ashlar.Infrastructure, which a caller references and
/// registers separately; pulling it in from Lite would defeat the slim purpose.</para>
/// </summary>
public static class AshlarLiteServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Ashlar API client for a Lite host.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiBaseUrl">Base URL of the Ashlar API (e.g. https://your-server:5000).</param>
    public static IServiceCollection AddAshlarLite(this IServiceCollection services, string apiBaseUrl)
    {
        services.AddAshlarClient(apiBaseUrl);
        return services;
    }
}

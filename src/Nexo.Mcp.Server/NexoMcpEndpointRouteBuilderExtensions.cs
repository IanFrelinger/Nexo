using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.Mcp.Server;

/// <summary>
/// Endpoint mapping for the MCP streamable-HTTP transport.
/// </summary>
public static class NexoMcpEndpointRouteBuilderExtensions
{
    /// <summary>Default route pattern. Kept under <c>/api</c> because the Nexo API key middleware
    /// only protects paths below <c>/api</c> — mapping elsewhere silently opts out of auth.</summary>
    public const string DefaultPattern = "/api/mcp";

    /// <summary>
    /// Maps the MCP endpoint when <see cref="NexoMcpServerOptions.Enabled"/> is set; otherwise maps
    /// nothing and returns <see langword="null"/> so the surface stays completely dark (requests
    /// fall through to the host's 404 handling).
    ///
    /// Unlike the commercial GameDirector mapping, this deliberately does NOT call
    /// <c>AllowAnonymous()</c> — the endpoint inherits whatever auth the host pipeline applies,
    /// and hosts are expected to add an explicit all-verbs auth filter plus rate limiting.
    /// </summary>
    public static IEndpointConventionBuilder? MapNexoMcpEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern = DefaultPattern)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<NexoMcpServerOptions>>().Value;
        if (!options.Enabled)
        {
            endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(NexoMcpEndpointRouteBuilderExtensions))
                .LogInformation("Nexo MCP server is disabled; {Pattern} not mapped.", pattern);
            return null;
        }

        return endpoints.MapMcp(pattern);
    }
}

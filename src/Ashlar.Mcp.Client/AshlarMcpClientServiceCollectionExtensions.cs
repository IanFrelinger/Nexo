using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions;

namespace Ashlar.Mcp.Client;

/// <summary>
/// DI composition for the MCP client. Host-composed (never kernel-registered) so the
/// ModelContextProtocol dependency stays out of the AddAshlar package graph. Call once per host.
/// </summary>
public static class AshlarMcpClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP client connection manager as a hosted service and as an
    /// <see cref="IToolSource"/>. Proxied tools reach agents through toolbox factories that fold
    /// tool sources in (see <c>RepoFsToolboxFactory</c>); nothing is dialed until
    /// <see cref="AshlarMcpClientOptions.Enabled"/> is set.
    /// </summary>
    public static IServiceCollection AddAshlarMcpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = AshlarMcpClientOptions.SectionPath)
    {
        services.AddOptions<AshlarMcpClientOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AshlarMcpClientOptions>, ValidateAshlarMcpClientOptions>());

        services.TryAddSingleton<McpClientConnectionManager>();
        // Same instance under all three roles: the hosted service owns connections, the tool
        // source exposes the discovered proxies. Factory registrations (not TryAddEnumerable,
        // which rejects factory descriptors) — AddAshlarMcpClient is documented call-once.
        services.AddSingleton<IToolSource>(sp => sp.GetRequiredService<McpClientConnectionManager>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<McpClientConnectionManager>());

        return services;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nexo.Abstractions;

namespace Nexo.Mcp.Client;

/// <summary>
/// DI composition for the MCP client. Host-composed (never kernel-registered) so the
/// ModelContextProtocol dependency stays out of the AddNexo package graph. Call once per host.
/// </summary>
public static class NexoMcpClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP client connection manager as a hosted service and as an
    /// <see cref="IToolSource"/>. Proxied tools reach agents through toolbox factories that fold
    /// tool sources in (see <c>RepoFsToolboxFactory</c>); nothing is dialed until
    /// <see cref="NexoMcpClientOptions.Enabled"/> is set.
    /// </summary>
    public static IServiceCollection AddNexoMcpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = NexoMcpClientOptions.SectionPath)
    {
        services.AddOptions<NexoMcpClientOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<NexoMcpClientOptions>, ValidateNexoMcpClientOptions>());

        services.TryAddSingleton<McpClientConnectionManager>();
        // Same instance under all three roles: the hosted service owns connections, the tool
        // source exposes the discovered proxies. Factory registrations (not TryAddEnumerable,
        // which rejects factory descriptors) — AddNexoMcpClient is documented call-once.
        services.AddSingleton<IToolSource>(sp => sp.GetRequiredService<McpClientConnectionManager>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<McpClientConnectionManager>());

        return services;
    }
}

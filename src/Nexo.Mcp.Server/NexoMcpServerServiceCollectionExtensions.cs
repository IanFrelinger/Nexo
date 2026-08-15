using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Nexo.Mcp.Server;

/// <summary>
/// DI composition for the Nexo MCP server bridge.
///
/// Deliberately host-composed rather than kernel-registered: endpoint/protocol exposure is a
/// host decision (mirroring the gRPC server precedent), and keeping the ModelContextProtocol
/// dependency out of Nexo.Hosting keeps it out of every AddNexo consumer's package graph.
/// </summary>
public static class NexoMcpServerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP server bridge and the underlying MCP server. The returned
    /// <see cref="IMcpServerBuilder"/> still needs a transport chosen by the host:
    /// <c>.WithHttpTransport()</c> (ModelContextProtocol.AspNetCore) for web hosts followed by
    /// <see cref="NexoMcpEndpointRouteBuilderExtensions.MapNexoMcpEndpoint"/>, or
    /// <c>.WithStdioServerTransport()</c> for console hosts.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration root to bind <see cref="NexoMcpServerOptions"/> from.</param>
    /// <param name="sectionPath">Configuration section (defaults to <c>Nexo:Mcp:Server</c>).</param>
    public static IMcpServerBuilder AddNexoMcpServer(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = NexoMcpServerOptions.SectionPath)
    {
        services.AddOptions<NexoMcpServerOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<NexoMcpServerOptions>, ValidateNexoMcpServerOptions>());

        // TryAdd for the seams hosts may legitimately replace (world model, policy gate);
        // TryAddEnumerable for contributors so host-added contributors compose instead of clobber.
        services.TryAddSingleton<IMcpWorldSnapshotFactory, OptionsMcpWorldSnapshotFactory>();
        services.TryAddSingleton<IMcpInvocationGate, PolicyMcpInvocationGate>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMcpToolContributor, ToolboxMcpToolContributor>());
        services.AddSingleton<NexoMcpToolBridge>();
        services.AddHostedService<McpCatalogStartupValidator>();

        // ServerInfo depends on bound NexoMcpServerOptions, so it is configured through the
        // options pipeline rather than captured at registration time.
        services.AddOptions<McpServerOptions>().Configure<IOptions<NexoMcpServerOptions>>((mcp, nexo) =>
        {
            mcp.ServerInfo = new Implementation
            {
                Name = nexo.Value.ServerName,
                Version = nexo.Value.ServerVersion ?? DefaultServerVersion(),
            };
        });

        // Dynamic list/call handlers instead of pre-materialized McpServerTool instances: the
        // catalog is derived from options + DI at runtime, and Nexo tools carry hand-authored
        // JSON schemas that reflection-based tool factories would mangle.
        return services.AddMcpServer()
            .WithListToolsHandler(static async (ctx, ct) =>
                await ResolveBridge(ctx.Services ?? ctx.Server.Services).ListToolsAsync(ct).ConfigureAwait(false))
            .WithCallToolHandler(static async (ctx, ct) =>
            {
                var parameters = ctx.Params
                    ?? throw new McpProtocolException("Missing tools/call parameters.", McpErrorCode.InvalidParams);
                return await ResolveBridge(ctx.Services ?? ctx.Server.Services)
                    .CallToolAsync(parameters, ctx.User, ct).ConfigureAwait(false);
            });
    }

    private static NexoMcpToolBridge ResolveBridge(IServiceProvider? services)
    {
        var provider = services
            ?? throw new InvalidOperationException(
                "No service provider available on the MCP request context; " +
                "AddNexoMcpServer must be used with the hosting integration.");
        return provider.GetRequiredService<NexoMcpToolBridge>();
    }

    private static string DefaultServerVersion() =>
        typeof(NexoMcpServerServiceCollectionExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";
}

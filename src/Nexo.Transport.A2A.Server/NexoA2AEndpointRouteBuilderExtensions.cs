using A2A;
using A2A.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Transport;

namespace Nexo.Transport.A2A.Server;

/// <summary>
/// Endpoint composition for the A2A server: one JSON-RPC endpoint + card per exposed agent
/// under <c>{base}/{agentId}</c>, plus the primary agent's card at the root
/// <c>/.well-known/agent-card.json</c> discovery path.
/// </summary>
public static class NexoA2AEndpointRouteBuilderExtensions
{
    /// <summary>Default route prefix. Kept under <c>/api</c> because the Nexo API key middleware
    /// only protects paths below <c>/api</c>.</summary>
    public const string DefaultBasePattern = "/api/a2a";

    /// <summary>
    /// Maps A2A endpoints for every exposed agent when
    /// <see cref="NexoA2AServerOptions.Enabled"/> is set; otherwise maps nothing and returns
    /// <see langword="null"/> so the surface stays completely dark.
    ///
    /// RPC and card endpoints are returned separately: hosts attach auth conventions to both,
    /// and may relax only the card group when
    /// <see cref="NexoA2AServerOptions.AllowAnonymousAgentCard"/> opts into public discovery.
    /// Task state is per-agent and in-memory (v1 synchronous model) — <c>tasks/get</c> works
    /// within the process lifetime only.
    /// </summary>
    public static NexoA2AEndpointConventions? MapNexoA2AEndpoints(
        this IEndpointRouteBuilder endpoints,
        string basePattern = DefaultBasePattern)
    {
        var services = endpoints.ServiceProvider;
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(NexoA2AEndpointRouteBuilderExtensions));

        var options = services.GetRequiredService<IOptions<NexoA2AServerOptions>>().Value;
        if (!options.Enabled)
        {
            logger.LogInformation("Nexo A2A server is disabled; no endpoints mapped under {BasePattern}.", basePattern);
            return null;
        }

        var catalog = services.GetRequiredService<INexoA2AAgentCatalog>();
        var projector = services.GetRequiredService<INexoA2ACardProjector>();
        var transport = services.GetRequiredService<IAgentTransport>();

        var exposed = NexoA2AExposurePolicy.FilterExposed(catalog.GetAgents(), options);
        var primary = NexoA2AExposurePolicy.ResolvePrimary(exposed, options);

        var rpcEndpoints = new List<IEndpointConventionBuilder>();
        var cardEndpoints = new List<IEndpointConventionBuilder>();
        foreach (var descriptor in exposed)
        {
            var handler = new NexoA2AAgentHandler(
                descriptor,
                transport,
                options.DefaultExecutionTimeout,
                loggerFactory.CreateLogger<NexoA2AAgentHandler>());

            // One A2AServer (and in-memory task store) per agent: task ids are scoped to the
            // agent endpoint, and a per-agent store means one agent's task volume cannot
            // starve another's.
            var server = new A2AServer(
                handler,
                new InMemoryTaskStore(),
                new ChannelEventNotifier(),
                loggerFactory.CreateLogger<A2AServer>(),
                new A2AServerOptions());

            var path = $"{basePattern}/{Uri.EscapeDataString(descriptor.Id)}";
            rpcEndpoints.Add(endpoints.MapA2A(server, path));
            cardEndpoints.Add(endpoints.MapWellKnownAgentCard(
                projector.Project(descriptor, options, basePattern), path));
        }

        cardEndpoints.Add(endpoints.MapWellKnownAgentCard(
            projector.Project(primary, options, basePattern)));

        logger.LogInformation(
            "Nexo A2A server mapped. Agents={AgentCount} Primary={PrimaryAgentId} Base={BasePattern}",
            exposed.Count, primary.Id, basePattern);

        return new NexoA2AEndpointConventions(rpcEndpoints, cardEndpoints, options.AllowAnonymousAgentCard);
    }
}

/// <summary>Mapped endpoint groups, split so hosts can apply different auth conventions.</summary>
/// <param name="RpcEndpoints">JSON-RPC task/message endpoints (always auth-gated by hosts).</param>
/// <param name="CardEndpoints">Agent-card discovery endpoints (root + per-agent).</param>
/// <param name="AllowAnonymousAgentCard">The operator's card-anonymity decision, surfaced for the host's auth wiring.</param>
public sealed record NexoA2AEndpointConventions(
    IReadOnlyList<IEndpointConventionBuilder> RpcEndpoints,
    IReadOnlyList<IEndpointConventionBuilder> CardEndpoints,
    bool AllowAnonymousAgentCard);

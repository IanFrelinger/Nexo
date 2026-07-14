using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Nexo.AI.Pipeline.Governance;

namespace Nexo.AI.Pipeline.Routing;

/// <summary>
/// Routes each call to a governed per-target <see cref="IChatClient"/> pipeline.
/// Routing sits outside per-target stacks; wrap this client with <see cref="AuditingChatClient"/>
/// so route decisions are audited.
/// </summary>
public sealed class RoutingChatClient : IChatClient
{
    /// <summary>DI / audit key for the router itself.</summary>
    public const string RouterTargetKey = "router:default";

    private readonly IChatRouter _router;
    private readonly IServiceProvider _services;
    private readonly Func<string?>? _callerIdentityAccessor;
    private readonly IChatInvocationAuditor? _auditor;

    /// <summary>Creates a routing client.</summary>
    public RoutingChatClient(
        IChatRouter router,
        IServiceProvider services,
        IChatInvocationAuditor? auditor = null,
        Func<string?>? callerIdentityAccessor = null)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _auditor = auditor;
        _callerIdentityAccessor = callerIdentityAccessor;
    }

    /// <summary>Last route decision (for tests / introspection).</summary>
    public RouteDecision? LastDecision { get; private set; }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var (client, decision) = ResolveClient(options);
        LastDecision = decision;
        RecordRoute(decision);
        return await client.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (client, decision) = ResolveClient(options);
        LastDecision = decision;
        RecordRoute(decision);
        await foreach (var update in client.GetStreamingResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
        {
            return new ChatClientMetadata("nexo-router", providerUri: null, defaultModelId: RouterTargetKey);
        }

        if (serviceType == typeof(RoutingChatClient))
        {
            return this;
        }

        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private (IChatClient Client, RouteDecision Decision) ResolveClient(ChatOptions? options)
    {
        var tier = RouteRequestHints.GetCapabilityTier(options);
        var preferred = RouteRequestHints.GetPreferredTarget(options);
        var decision = _router.Select(_callerIdentityAccessor?.Invoke(), options?.ModelId, tier, preferred);

        if (string.IsNullOrEmpty(decision.ChosenTargetKey)
            || decision.ReasonCode == RouteReasonCodes.NoCandidate)
        {
            throw new PolicyViolationException(
                code: RouteReasonCodes.NoCandidate,
                message: "No permitted and available execution target can satisfy this request.",
                modelId: options?.ModelId,
                details: new Dictionary<string, string>
                {
                    ["tier"] = tier.ToString(),
                    ["candidates"] = string.Join(',', decision.CandidatesConsidered.Select(c => c.TargetKey)),
                });
        }

        var client = _services.GetRequiredKeyedService<IChatClient>(decision.ChosenTargetKey);
        return (client, decision);
    }

    private void RecordRoute(RouteDecision decision)
    {
        if (_auditor is null)
        {
            return;
        }

        _auditor.Record(new ChatInvocationAuditRecord
        {
            TargetKey = RouterTargetKey,
            Outcome = string.IsNullOrEmpty(decision.ChosenTargetKey) ? "denied" : "routed",
            PolicyDecisions = new[]
            {
                $"chosen={decision.ChosenTargetKey}",
                $"reason={decision.ReasonCode}",
                $"tier={decision.RequestedTier}",
                $"candidates={string.Join(';', decision.CandidatesConsidered.Select(c => $"{c.TargetKey}:{c.Disposition}"))}",
            },
            ReasonCode = decision.ReasonCode,
        });
    }
}

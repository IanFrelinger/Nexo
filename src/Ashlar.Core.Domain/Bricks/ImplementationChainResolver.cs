using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// Everything the resolver is allowed to see when deciding how a brick runs.
/// </summary>
/// <param name="Brick">The brick being executed.</param>
/// <param name="Context">Execution context (air-gapped, provider, audit mode).</param>
/// <param name="Preferred">
/// Caller-forced implementation, or <see cref="ImplementationType.Auto"/> to let
/// the brick's own declaration decide. Auto is what lets
/// <see cref="Brick.Selector"/> have a say.
/// </param>
/// <param name="RuntimeSpec">
/// Optional per-brick hot-swap spec. Its Prefer/Fallback win over the brick's own
/// defaults when supplied.
/// </param>
/// <param name="IsAvailable">
/// Availability filter. Null means "the brick declares that implementation".
/// Executors that additionally require a live provider pass their own predicate —
/// this stays injectable ON PURPOSE, because the three executors legitimately
/// disagree about whether provider reachability is checked here or left to the
/// call to fail and fall back.
/// </param>
public sealed record ImplementationChainRequest(
    Brick Brick,
    IExecutionContext Context,
    ImplementationType Preferred = ImplementationType.Auto,
    BrickRuntimeSpec? RuntimeSpec = null,
    Func<Brick, ImplementationType, IExecutionContext, bool>? IsAvailable = null);

/// <summary>
/// Resolves the ordered list of implementations to try for a brick.
/// </summary>
public interface IImplementationChainResolver
{
    /// <summary>Resolves the ordered, availability-filtered chain.</summary>
    /// <param name="request">Brick, context, and any caller overrides.</param>
    IReadOnlyList<ImplementationType> Resolve(ImplementationChainRequest request);
}

/// <summary>
/// The single implementation-selection rule for the whole framework.
///
/// Before this existed, BehaviorExecutor, ClusterExecutor and WorkflowExecutor
/// each had their own copy of "pick a head, append the fallback chain, drop what
/// is unavailable". They had drifted: WorkflowExecutor never consulted
/// <see cref="Brick.Selector"/>, so a brick that declared a selector resolved one
/// way under a behavior or a cluster and a different way under a workflow. The
/// declaration was silently ignored in one of the three, which is the worst kind
/// of inconsistency — nothing errors, the brick just runs the other
/// implementation.
///
/// Order of authority, highest first:
///   1. air-gapped  -> deterministic only, no exceptions
///   2. caller-forced <see cref="ImplementationChainRequest.Preferred"/>
///   3. runtime spec Prefer ("agentic"/"deterministic")
///   4. the brick's own <see cref="Brick.Selector"/>
///   5. the brick's <see cref="Brick.DefaultImplementation"/>
/// then the fallback chain (runtime spec's if supplied, else the brick's),
/// de-duplicated, then filtered by availability.
/// </summary>
public sealed class ImplementationChainResolver : IImplementationChainResolver
{
    /// <summary>Shared stateless instance.</summary>
    public static ImplementationChainResolver Instance { get; } = new();

    /// <summary>Default availability: the brick declares that implementation.</summary>
    public static bool DeclaredOnly(Brick brick, ImplementationType type, IExecutionContext context) =>
        type switch
        {
            ImplementationType.Deterministic => brick.Implementations.HasDeterministic,
            ImplementationType.Agentic => brick.Implementations.HasAgentic,
            _ => false
        };

    /// <inheritdoc />
    public IReadOnlyList<ImplementationType> Resolve(ImplementationChainRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var brick = request.Brick;
        var context = request.Context;
        var isAvailable = request.IsAvailable ?? DeclaredOnly;

        // Air-gapped is absolute: no selector, spec, or caller preference may
        // route work to an agentic implementation with no network.
        if (context.IsAirGapped)
        {
            return isAvailable(brick, ImplementationType.Deterministic, context)
                ? new[] { ImplementationType.Deterministic }
                : Array.Empty<ImplementationType>();
        }

        var head = ResolveHead(request);

        var chain = new List<ImplementationType>();
        if (head != ImplementationType.Auto)
            chain.Add(head);

        foreach (var fallback in request.RuntimeSpec?.Fallback ?? brick.FallbackChain)
        {
            if (!chain.Contains(fallback))
                chain.Add(fallback);
        }

        // Nothing resolved to a concrete head and no fallbacks: fall back to the
        // brick's own declaration rather than returning an empty chain.
        if (chain.Count == 0)
        {
            chain.Add(brick.DefaultImplementation);
            foreach (var fallback in brick.FallbackChain)
            {
                if (!chain.Contains(fallback))
                    chain.Add(fallback);
            }
        }

        return chain.Where(t => isAvailable(brick, t, context)).ToList();
    }

    private static ImplementationType ResolveHead(ImplementationChainRequest request)
    {
        // A caller that named an implementation outranks everything below.
        if (request.Preferred != ImplementationType.Auto)
            return request.Preferred;

        if (request.RuntimeSpec is { } spec)
        {
            switch ((spec.Prefer ?? "auto").Trim().ToLowerInvariant())
            {
                case "deterministic":
                    return ImplementationType.Deterministic;
                case "agentic":
                    return ImplementationType.Agentic;
            }
        }

        // THE FIX: the brick's declared selector is consulted here, so every
        // executor honours it. WorkflowExecutor used to skip straight to
        // DefaultImplementation and silently ignore this.
        if (request.Brick.Selector is { } selector)
            return selector.Select(request.Context);

        return request.Brick.DefaultImplementation;
    }
}

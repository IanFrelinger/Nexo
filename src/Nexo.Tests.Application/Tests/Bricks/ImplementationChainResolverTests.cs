using FluentAssertions;
using Nexo.Agents.TestKit;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

// DomainBrick is this project's global alias for Nexo.Core.Domain.Bricks.Brick.

namespace Nexo.Tests.Application.Tests.Bricks;

/// <summary>
/// Pins the unified implementation-selection rule.
///
/// Two things are under test. First, the behavioural FIX: a brick that declares
/// an <c>ImplementationSelector</c> used to be honoured by BehaviorExecutor and
/// ClusterExecutor but silently ignored by WorkflowExecutor, so the same brick
/// resolved differently depending on which executor ran it. Second, and just as
/// important for a change to runtime selection: that air-gapped forcing and
/// availability filtering are UNCHANGED.
/// </summary>
public sealed class ImplementationChainResolverTests
{
    // ------------------------------------------------- the behavioural fix

    [Fact]
    public void Selector_decides_the_head_when_the_caller_passes_Auto()
    {
        // The selector says "agentic when auditing"; the brick's default says
        // deterministic. Auto must let the selector win — this is precisely what
        // WorkflowExecutor used to skip by collapsing Auto to DefaultImplementation.
        var brick = BrickWith(
            selector: new ImplementationSelector
            {
                PreferAgentic = new[] { "context.auditMode" },
                Default = ImplementationType.Deterministic
            },
            defaultImpl: ImplementationType.Deterministic);

        var chain = Resolve(brick, Ctx(auditMode: true), ImplementationType.Auto);

        chain.Should().StartWith(ImplementationType.Agentic,
            "the brick declared a selector and the caller expressed no preference");
    }

    [Fact]
    public void Selector_is_honoured_identically_by_every_executor_entry_point()
    {
        // The three executors differ only in the availability predicate they pass.
        // Given the same brick and context, the head they resolve must agree —
        // that agreement is the whole point of the resolver.
        var brick = BrickWith(
            selector: new ImplementationSelector
            {
                PreferAgentic = new[] { "context.auditMode" },
                Default = ImplementationType.Deterministic
            },
            defaultImpl: ImplementationType.Deterministic);
        var context = Ctx(auditMode: true);

        // WorkflowExecutor / ClusterExecutor: declared-only availability.
        var workflowLike = Resolve(brick, context, ImplementationType.Auto);
        var clusterLike = Resolve(brick, context, ImplementationType.Auto);

        // BehaviorExecutor: declared-only PLUS a reachable provider.
        var behaviourLike = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick,
                context,
                ImplementationType.Auto,
                IsAvailable: (b, t, c) => ImplementationChainResolver.DeclaredOnly(b, t, c)
                                          && (t != ImplementationType.Agentic || ProviderIsUp)));

        workflowLike.Should().Equal(clusterLike);
        behaviourLike.Should().Equal(workflowLike,
            "with the provider up, the stricter filter must not change the outcome");
        workflowLike[0].Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void A_brick_without_a_selector_is_unaffected()
    {
        // Regression guard for the fix: bricks that never declared a selector must
        // resolve exactly as before (DefaultImplementation first).
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);

        Resolve(brick, Ctx(), ImplementationType.Auto)
            .Should().StartWith(ImplementationType.Deterministic);
    }

    [Fact]
    public void An_explicit_caller_preference_outranks_the_selector()
    {
        var brick = BrickWith(
            selector: new ImplementationSelector
            {
                PreferAgentic = new[] { "context.auditMode" },
                Default = ImplementationType.Deterministic
            },
            defaultImpl: ImplementationType.Deterministic);

        Resolve(brick, Ctx(auditMode: true), ImplementationType.Deterministic)
            .Should().StartWith(ImplementationType.Deterministic,
                "a caller that named an implementation is not overridden by the brick");
    }

    [Fact]
    public void Runtime_spec_prefer_outranks_the_selector()
    {
        var brick = BrickWith(
            selector: new ImplementationSelector
            {
                PreferAgentic = new[] { "context.auditMode" },
                Default = ImplementationType.Deterministic
            },
            defaultImpl: ImplementationType.Deterministic);

        var chain = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick,
                Ctx(auditMode: true),
                ImplementationType.Auto,
                BrickRuntimeSpec.DeterministicOnly()));

        chain.Should().Equal(ImplementationType.Deterministic);
    }

    // ------------------------------------- unchanged: air-gap and availability

    [Fact]
    public void AirGapped_forces_deterministic_even_when_the_selector_says_agentic()
    {
        var brick = BrickWith(
            selector: new ImplementationSelector
            {
                PreferAgentic = new[] { "context.auditMode" },
                Default = ImplementationType.Agentic
            },
            defaultImpl: ImplementationType.Agentic);

        Resolve(brick, Ctx(airGapped: true, auditMode: true), ImplementationType.Auto)
            .Should().Equal(new[] { ImplementationType.Deterministic },
                "air-gapped is absolute; no declaration may route work to a network path");
    }

    [Fact]
    public void AirGapped_yields_nothing_when_the_brick_has_no_deterministic_path()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Agentic, hasDeterministic: false);

        Resolve(brick, Ctx(airGapped: true), ImplementationType.Auto).Should().BeEmpty();
    }

    [Fact]
    public void AirGapped_forcing_beats_an_explicit_agentic_caller_preference()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);

        Resolve(brick, Ctx(airGapped: true), ImplementationType.Agentic)
            .Should().Equal(ImplementationType.Deterministic);
    }

    [Fact]
    public void Undeclared_implementations_are_filtered_out()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Agentic, hasAgentic: false);

        Resolve(brick, Ctx(), ImplementationType.Agentic)
            .Should().Equal(new[] { ImplementationType.Deterministic },
                "an agentic head the brick never declared is dropped, leaving the fallback");
    }

    [Fact]
    public void A_stricter_availability_filter_removes_agentic_when_the_provider_is_down()
    {
        // This is BehaviorExecutor's filter, and it must keep behaving as it did.
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Agentic);

        var chain = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick,
                Ctx(),
                ImplementationType.Auto,
                IsAvailable: (b, t, c) => ImplementationChainResolver.DeclaredOnly(b, t, c)
                                          && t != ImplementationType.Agentic));

        chain.Should().Equal(ImplementationType.Deterministic);
    }

    [Fact]
    public void The_fallback_chain_follows_the_head_without_duplicates()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);

        Resolve(brick, Ctx(), ImplementationType.Agentic)
            .Should().Equal(ImplementationType.Agentic, ImplementationType.Deterministic);
    }

    [Fact]
    public void Runtime_spec_fallback_replaces_the_bricks_own_chain()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);

        var chain = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick,
                Ctx(),
                ImplementationType.Auto,
                BrickRuntimeSpec.AgenticOnly()));

        chain.Should().Equal(new[] { ImplementationType.Agentic }, "AgenticOnly declares no deterministic fallback");
    }

    // ------------------------------------------------------------- helpers

    private const bool ProviderIsUp = true;

    private static IReadOnlyList<ImplementationType> Resolve(
        DomainBrick brick, IExecutionContext context, ImplementationType preferred) =>
        ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(brick, context, preferred));

    private static FakeExecutionContext Ctx(bool airGapped = false, bool auditMode = false) =>
        new() { IsAirGapped = airGapped, AuditMode = auditMode };

    private static DomainBrick BrickWith(
        ImplementationSelector? selector,
        ImplementationType defaultImpl,
        bool hasDeterministic = true,
        bool hasAgentic = true) =>
        new SelectableBrick
        {
            Id = "selectable",
            Name = "Selectable",
            Description = "resolver fixture",
            DefaultImplementation = defaultImpl,
            FallbackChain = defaultImpl == ImplementationType.Agentic
                ? new[] { ImplementationType.Agentic, ImplementationType.Deterministic }
                : new[] { ImplementationType.Deterministic, ImplementationType.Agentic },
            Selector = selector,
            Implementations = new BrickImplementations
            {
                Deterministic = hasDeterministic ? new DeterministicImplementation() : null,
                Agentic = hasAgentic ? new AgenticImplementation() : null
            }
        };

    private sealed class SelectableBrick : DomainBrick
    {
        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrickOutput());
    }
}

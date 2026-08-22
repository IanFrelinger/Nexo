using FluentAssertions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Xunit;

// DomainBrick is this project's established alias for Ashlar.Core.Domain.Bricks.Brick:
// the enclosing namespace ends in .Bricks, which would otherwise shadow the type.

namespace Ashlar.Tests.Domain.Tests.Bricks;

/// <summary>
/// Coverage-complete tests for <see cref="ImplementationChainResolver"/>, the
/// single implementation-selection rule.
///
/// These live in Ashlar.Tests.Domain — not only in Ashlar.Tests.Application where the
/// behavioural matrix also lives — because Ashlar.Core.Domain is gated at 100% line
/// coverage measured from THIS assembly. The resolver is domain code, so its line
/// coverage has to come from the domain suite; putting the tests only next to the
/// executors that call it left the gate at 94%.
/// </summary>
public sealed class ImplementationChainResolverTests
{
    // ------------------------------------------------------------ head order

    [Fact]
    public void Selector_decides_the_head_when_the_caller_passes_Auto()
    {
        var brick = BrickWith(
            selector: new ImplementationSelector
            {
                PreferAgentic = new[] { "context.auditMode" },
                Default = ImplementationType.Deterministic
            },
            defaultImpl: ImplementationType.Deterministic);

        Resolve(brick, Ctx(auditMode: true), ImplementationType.Auto)
            .Should().StartWith(ImplementationType.Agentic);
    }

    [Fact]
    public void Without_a_selector_the_bricks_default_is_the_head()
    {
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
            .Should().StartWith(ImplementationType.Deterministic);
    }

    [Theory]
    [InlineData("deterministic", ImplementationType.Deterministic)]
    [InlineData("agentic", ImplementationType.Agentic)]
    [InlineData("DETERMINISTIC", ImplementationType.Deterministic)]
    [InlineData("  agentic  ", ImplementationType.Agentic)]
    public void Runtime_spec_prefer_outranks_the_selector(string prefer, ImplementationType expected)
    {
        var brick = BrickWith(
            selector: new ImplementationSelector { Default = ImplementationType.Deterministic },
            defaultImpl: ImplementationType.Deterministic);

        var chain = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick,
                Ctx(),
                ImplementationType.Auto,
                new BrickRuntimeSpec
                {
                    Prefer = prefer,
                    Fallback = new[] { ImplementationType.Deterministic, ImplementationType.Agentic }
                }));

        chain.Should().StartWith(expected);
    }

    [Fact]
    public void An_unrecognised_runtime_spec_prefer_falls_through_to_the_selector()
    {
        // "auto" (and anything unknown) must not short-circuit the selector.
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
                new BrickRuntimeSpec
                {
                    Prefer = "auto",
                    Fallback = new[] { ImplementationType.Agentic, ImplementationType.Deterministic }
                }));

        chain.Should().StartWith(ImplementationType.Agentic);
    }

    // ------------------------------------------------------------- air-gapped

    [Fact]
    public void AirGapped_forces_deterministic_even_when_the_selector_says_agentic()
    {
        var brick = BrickWith(
            selector: new ImplementationSelector { Default = ImplementationType.Agentic },
            defaultImpl: ImplementationType.Agentic);

        Resolve(brick, Ctx(airGapped: true), ImplementationType.Auto)
            .Should().Equal(new[] { ImplementationType.Deterministic });
    }

    [Fact]
    public void AirGapped_yields_nothing_when_the_brick_has_no_deterministic_path()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Agentic, hasDeterministic: false);

        Resolve(brick, Ctx(airGapped: true), ImplementationType.Auto).Should().BeEmpty();
    }

    // ------------------------------------------------- chain build + filter

    [Fact]
    public void The_fallback_chain_follows_the_head_without_duplicates()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);

        Resolve(brick, Ctx(), ImplementationType.Agentic)
            .Should().Equal(new[] { ImplementationType.Agentic, ImplementationType.Deterministic });
    }

    [Fact]
    public void Runtime_spec_fallback_replaces_the_bricks_own_chain()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);

        var chain = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick, Ctx(), ImplementationType.Auto, BrickRuntimeSpec.AgenticOnly()));

        chain.Should().Equal(new[] { ImplementationType.Agentic });
    }

    [Fact]
    public void Undeclared_implementations_are_filtered_out()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Agentic, hasAgentic: false);

        Resolve(brick, Ctx(), ImplementationType.Agentic)
            .Should().Equal(new[] { ImplementationType.Deterministic });
    }

    [Fact]
    public void An_all_Auto_brick_with_no_fallbacks_resolves_to_nothing()
    {
        // Exercises the "chain came out empty" recovery branch: head resolves to
        // Auto (no selector, Auto default) and the fallback list is empty, so the
        // resolver re-seeds from the brick's own declaration — which is still
        // Auto, and Auto is never an available implementation.
        var brick = BrickWith(
            selector: null,
            defaultImpl: ImplementationType.Auto,
            fallback: Array.Empty<ImplementationType>());

        Resolve(brick, Ctx(), ImplementationType.Auto).Should().BeEmpty();
    }

    [Fact]
    public void An_empty_runtime_spec_fallback_re_seeds_from_the_bricks_own_chain()
    {
        // The other half of the recovery branch: a runtime spec supplies an EMPTY
        // fallback list and the head resolves to Auto, so the spec contributes
        // nothing — the resolver then re-seeds from the brick's own declaration
        // and its fallback chain rather than giving up.
        var brick = BrickWith(
            selector: null,
            defaultImpl: ImplementationType.Auto,
            fallback: new[] { ImplementationType.Deterministic, ImplementationType.Agentic });

        var chain = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick,
                Ctx(),
                ImplementationType.Auto,
                new BrickRuntimeSpec { Prefer = "auto", Fallback = Array.Empty<ImplementationType>() }));

        chain.Should().Equal(new[] { ImplementationType.Deterministic, ImplementationType.Agentic });
    }

    [Fact]
    public void A_custom_availability_filter_is_honoured()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Agentic);

        var chain = ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(
                brick,
                Ctx(),
                ImplementationType.Auto,
                IsAvailable: (b, t, c) => ImplementationChainResolver.DeclaredOnly(b, t, c)
                                          && t != ImplementationType.Agentic));

        chain.Should().Equal(new[] { ImplementationType.Deterministic });
    }

    // --------------------------------------------------------- DeclaredOnly

    [Theory]
    [InlineData(ImplementationType.Deterministic, true)]
    [InlineData(ImplementationType.Agentic, true)]
    [InlineData(ImplementationType.Auto, false)]
    public void DeclaredOnly_reports_what_the_brick_declares(ImplementationType type, bool expected)
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);

        ImplementationChainResolver.DeclaredOnly(brick, type, Ctx()).Should().Be(expected);
    }

    [Fact]
    public void DeclaredOnly_is_false_for_implementations_the_brick_lacks()
    {
        var brick = BrickWith(
            selector: null,
            defaultImpl: ImplementationType.Deterministic,
            hasDeterministic: false,
            hasAgentic: false);

        ImplementationChainResolver.DeclaredOnly(brick, ImplementationType.Deterministic, Ctx()).Should().BeFalse();
        ImplementationChainResolver.DeclaredOnly(brick, ImplementationType.Agentic, Ctx()).Should().BeFalse();
    }

    // ----------------------------------------------------------- guard rails

    [Fact]
    public void A_null_request_is_rejected()
    {
        var act = () => ImplementationChainResolver.Instance.Resolve(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void The_request_carries_its_inputs()
    {
        var brick = BrickWith(selector: null, defaultImpl: ImplementationType.Deterministic);
        var context = Ctx(auditMode: true);
        var spec = BrickRuntimeSpec.DeterministicOnly();

        var request = new ImplementationChainRequest(brick, context, ImplementationType.Agentic, spec);

        request.Brick.Should().BeSameAs(brick);
        request.Context.Should().BeSameAs(context);
        request.Preferred.Should().Be(ImplementationType.Agentic);
        request.RuntimeSpec.Should().BeSameAs(spec);
        request.IsAvailable.Should().BeNull("an unset filter means declared-only");
    }

    // ------------------------------------------------------------- helpers

    private static IReadOnlyList<ImplementationType> Resolve(
        DomainBrick brick, IExecutionContext context, ImplementationType preferred) =>
        ImplementationChainResolver.Instance.Resolve(
            new ImplementationChainRequest(brick, context, preferred));

    private static StubContext Ctx(bool airGapped = false, bool auditMode = false) =>
        new() { IsAirGapped = airGapped, AuditMode = auditMode };

    private static DomainBrick BrickWith(
        ImplementationSelector? selector,
        ImplementationType defaultImpl,
        bool hasDeterministic = true,
        bool hasAgentic = true,
        IReadOnlyList<ImplementationType>? fallback = null) =>
        new SelectableBrick
        {
            Id = "selectable",
            Name = "Selectable",
            Description = "resolver fixture",
            DefaultImplementation = defaultImpl,
            FallbackChain = fallback ?? (defaultImpl == ImplementationType.Agentic
                ? new[] { ImplementationType.Agentic, ImplementationType.Deterministic }
                : new[] { ImplementationType.Deterministic, ImplementationType.Agentic }),
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

    private sealed class StubContext : IExecutionContext
    {
        public string AgentId => "domain-test";
        public string BehaviorId => "domain-test";
        public bool IsAirGapped { get; init; }
        public bool AuditMode { get; init; }
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}

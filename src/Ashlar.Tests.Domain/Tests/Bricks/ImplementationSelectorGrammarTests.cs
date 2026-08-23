using FluentAssertions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Xunit;

namespace Ashlar.Tests.Domain.Tests.Bricks;

/// <summary>
/// Grammar coverage for <see cref="ImplementationSelector"/>.
///
/// <para>Before this, the selector recognised only "environment.airGapped" and
/// "context.auditMode"; every other authored condition fell through to false, so a brick's
/// real routing silently no-opped. These tests pin the three condition forms the shipped
/// OWASP scanner brick actually declares — bare boolean, equality, and membership — resolved
/// against the execution context, so that regression cannot recur unnoticed.</para>
/// </summary>
public sealed class ImplementationSelectorGrammarTests
{
    private sealed class Ctx : IExecutionContext
    {
        public string AgentId => "grammar-test";
        public string BehaviorId => "grammar-test";
        public bool IsAirGapped { get; init; }
        public bool AuditMode { get; init; }
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
    }

    private static ImplementationSelector OwaspLikeSelector() => new()
    {
        // Copied from OWASPScannerBrick's real Selector.
        PreferDeterministic =
        [
            "environment.airGapped",
            "context.auditMode",
            "input.language in ['csharp', 'java', 'javascript']",
        ],
        PreferAgentic =
        [
            "input.language in ['solidity', 'move', 'cairo']",
            "context.includeLogicFlaws",
            "context.depth == 'deep'",
        ],
        Default = ImplementationType.Deterministic,
    };

    [Theory]
    [InlineData("csharp", ImplementationType.Deterministic)]
    [InlineData("JavaScript", ImplementationType.Deterministic)]   // case-insensitive
    [InlineData("solidity", ImplementationType.Agentic)]
    [InlineData("move", ImplementationType.Agentic)]
    public void Membership_condition_routes_by_language(string language, ImplementationType expected)
    {
        var ctx = new Ctx { Variables = new Dictionary<string, object> { ["input.language"] = language } };

        OwaspLikeSelector().Select(ctx).Should().Be(expected);
    }

    [Fact]
    public void Membership_resolves_by_leaf_key_when_full_path_is_absent()
    {
        // The condition names "input.language"; a host that stored just "language" still works.
        var ctx = new Ctx { Variables = new Dictionary<string, object> { ["language"] = "solidity" } };

        OwaspLikeSelector().Select(ctx).Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void Equality_condition_routes_by_depth()
    {
        var ctx = new Ctx { Variables = new Dictionary<string, object> { ["context.depth"] = "deep" } };

        OwaspLikeSelector().Select(ctx).Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void Equality_condition_does_not_match_a_different_value()
    {
        var ctx = new Ctx { Variables = new Dictionary<string, object> { ["context.depth"] = "shallow" } };

        // No condition matches → Default.
        OwaspLikeSelector().Select(ctx).Should().Be(ImplementationType.Deterministic);
    }

    [Fact]
    public void Bare_boolean_variable_condition_routes()
    {
        var ctx = new Ctx { Variables = new Dictionary<string, object> { ["context.includeLogicFlaws"] = true } };

        OwaspLikeSelector().Select(ctx).Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void The_original_two_conditions_still_work()
    {
        new ImplementationSelector { PreferAgentic = ["environment.airGapped"] }
            .Select(new Ctx { IsAirGapped = true }).Should().Be(ImplementationType.Agentic);

        new ImplementationSelector { PreferAgentic = ["context.auditMode == true"] }
            .Select(new Ctx { AuditMode = true }).Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void Unrecognised_grammar_falls_through_to_default_without_throwing()
    {
        // Genuinely unsupported syntax must not throw; it falls to Default.
        var selector = new ImplementationSelector
        {
            PreferAgentic = ["some.weird || expression && nonsense"],
            Default = ImplementationType.Deterministic,
        };

        selector.Invoking(s => s.Select(new Ctx()))
            .Should().NotThrow();
        selector.Select(new Ctx()).Should().Be(ImplementationType.Deterministic);
    }
}

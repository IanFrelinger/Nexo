using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Policies;

/// <summary>
/// Unit tests for <see cref="BuildTestBudget"/> — ensures the planner can't
/// pin the cycle deadline by repeatedly invoking <c>dotnet.build</c> /
/// <c>dotnet.test</c>, and that <see cref="BuildTestBudget.Reset"/> properly
/// re-arms the budget between cycles.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BuildTestBudgetTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    private static ToolCall Call(string id) =>
        new(id, JsonSerializer.SerializeToElement(new { root = "." }));

    [Fact]
    public void Allows_calls_within_budget()
    {
        var policy = new BuildTestBudget(buildBudget: 1, testBudget: 2);

        policy.Approve(Call("dotnet.build"), EmptySnapshot, out _).Should().BeTrue();
        policy.Approve(Call("dotnet.test"), EmptySnapshot, out _).Should().BeTrue();
        policy.Approve(Call("dotnet.test"), EmptySnapshot, out _).Should().BeTrue();
    }

    [Fact]
    public void Rejects_when_build_budget_exceeded_and_includes_helpful_reason()
    {
        var policy = new BuildTestBudget(buildBudget: 1, testBudget: 99);
        policy.Approve(Call("dotnet.build"), EmptySnapshot, out _).Should().BeTrue();

        var ok = policy.Approve(Call("dotnet.build"), EmptySnapshot, out var reason);
        ok.Should().BeFalse();
        reason.Should().Contain("Build budget exceeded");
        reason.Should().Contain("inspect previous build output");
    }

    [Fact]
    public void Rejects_when_test_budget_exceeded()
    {
        var policy = new BuildTestBudget(buildBudget: 99, testBudget: 1);
        policy.Approve(Call("dotnet.test"), EmptySnapshot, out _).Should().BeTrue();
        policy.Approve(Call("dotnet.test"), EmptySnapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("Test budget exceeded");
    }

    [Fact]
    public void Reset_re_arms_budget_for_next_cycle()
    {
        var policy = new BuildTestBudget(buildBudget: 1, testBudget: 1);
        policy.Approve(Call("dotnet.build"), EmptySnapshot, out _).Should().BeTrue();
        policy.Approve(Call("dotnet.build"), EmptySnapshot, out _).Should().BeFalse();

        policy.Reset();

        policy.Approve(Call("dotnet.build"), EmptySnapshot, out _).Should()
            .BeTrue("Reset must clear the per-cycle counter so the next cycle gets a fresh budget");
    }

    [Fact]
    public void Ignores_unrelated_tool_calls()
    {
        var policy = new BuildTestBudget(buildBudget: 0, testBudget: 0);
        policy.Approve(Call("repo.fs.write"), EmptySnapshot, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Default_budgets_match_documented_constants()
    {
        // Regression test: changing the public constants is an API behaviour change
        // since operators wire NEXO_BUILD_BUDGET / NEXO_TEST_BUDGET against them.
        BuildTestBudget.DefaultBuildBudget.Should().Be(1);
        BuildTestBudget.DefaultTestBudget.Should().Be(2);
    }
}

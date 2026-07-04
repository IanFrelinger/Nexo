using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Policies;

/// <summary>Tests for build test budget gap coverage.</summary>
[Trait("Category", "Unit")]
public sealed class BuildTestBudgetGapCoverageTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    /// <summary>Call.</summary>
    /// <param name="id">Id.</param>
    private static ToolCall Call(string id) =>
        new(id, JsonSerializer.SerializeToElement(new { root = "." }));

    [Fact]
    public void Constructor_reads_build_budget_from_environment()
    {
        Environment.SetEnvironmentVariable("NEXO_BUILD_BUDGET", "2");
        Environment.SetEnvironmentVariable("NEXO_TEST_BUDGET", "99");
        try
        {
            var policy = new BuildTestBudget();

            policy.Approve(Call("dotnet.build"), EmptySnapshot, out _).Should().BeTrue();
            policy.Approve(Call("dotnet.build"), EmptySnapshot, out _).Should().BeTrue();
            policy.Approve(Call("dotnet.build"), EmptySnapshot, out var reason).Should().BeFalse();
            reason.Should().Contain("Build budget exceeded");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_BUILD_BUDGET", null);
            Environment.SetEnvironmentVariable("NEXO_TEST_BUDGET", null);
        }
    }

    [Fact]
    public void Constructor_falls_back_when_environment_budget_is_invalid()
    {
        Environment.SetEnvironmentVariable("NEXO_TEST_BUDGET", "-5");
        try
        {
            var policy = new BuildTestBudget(buildBudget: 99);

            policy.Approve(Call("dotnet.test"), EmptySnapshot, out _).Should().BeTrue();
            policy.Approve(Call("dotnet.test"), EmptySnapshot, out _).Should().BeTrue();
            policy.Approve(Call("dotnet.test"), EmptySnapshot, out var reason).Should().BeFalse();
            reason.Should().Contain("Test budget exceeded");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_TEST_BUDGET", null);
        }
    }
}

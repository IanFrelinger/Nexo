using System.Text.Json;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Policies.Dev;
using Xunit;

namespace Ashlar.Tests.Kernel;

[Collection("EnvironmentSensitive")]
public class BuildTestBudgetTests
{
    private static ToolCall Call(string id) => new(id, JsonDocument.Parse("{}").RootElement);
    private static readonly WorldSnapshot Snap = new(0, new Dictionary<string, object?>());

    [Fact]
    public void Default_constructor_uses_constants_when_env_unset()
    {
        EnvVar.Run("ASHLAR_BUILD_BUDGET", null, () =>
        EnvVar.Run("ASHLAR_TEST_BUDGET", null, () =>
        {
            var p = new BuildTestBudget();
            p.Approve(Call("dotnet.build"), Snap, out var r).Should().BeTrue();
            r.Should().Be("OK");
            p.Approve(Call("dotnet.build"), Snap, out var r2).Should().BeFalse();
            r2.Should().Contain("Build budget exceeded");
        }));
    }

    [Fact]
    public void Reads_environment_variables_when_set()
    {
        EnvVar.Run("ASHLAR_BUILD_BUDGET", " 3 ", () =>
        {
            var p = new BuildTestBudget(testBudget: 99);
            p.Approve(Call("dotnet.build"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.build"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.build"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.build"), Snap, out var reason).Should().BeFalse();
            reason.Should().Contain("Build budget exceeded");
        });
    }

    [Fact]
    public void Falls_back_when_environment_variable_is_invalid()
    {
        EnvVar.Run("ASHLAR_TEST_BUDGET", "not-a-number", () =>
        {
            var p = new BuildTestBudget(buildBudget: 99);
            for (var i = 0; i < BuildTestBudget.DefaultTestBudget; i++)
                p.Approve(Call("dotnet.test"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.test"), Snap, out var reason).Should().BeFalse();
            reason.Should().Contain("Test budget exceeded");
        });
    }

    [Fact]
    public void Allows_unrelated_tool_calls_indefinitely()
    {
        var p = new BuildTestBudget(0, 0);
        for (var i = 0; i < 10; i++)
        {
            p.Approve(Call("repo.fs.write"), Snap, out var r).Should().BeTrue();
            r.Should().Be("OK");
        }
    }

    [Fact]
    public void Reset_clears_counters()
    {
        var p = new BuildTestBudget(1, 1);
        p.Approve(Call("forge.build"), Snap, out _).Should().BeTrue();
        p.Approve(Call("forge.build"), Snap, out _).Should().BeFalse();
        p.Reset();
        p.Approve(Call("forge.build"), Snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Test_budget_enforces_separate_counter()
    {
        var p = new BuildTestBudget(0, 1);
        p.Approve(Call("forge.test"), Snap, out _).Should().BeTrue();
        p.Approve(Call("dotnet.test"), Snap, out var reason).Should().BeFalse();
        reason.Should().Contain("Test budget exceeded");
    }
}

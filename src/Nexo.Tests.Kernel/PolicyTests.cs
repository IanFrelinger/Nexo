using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies;
using Xunit;

namespace Nexo.Tests.Kernel;

public class AllowAllPolicyTests
{
    [Fact]
    public void Approve_always_returns_true_with_OK_reason()
    {
        var policy = new AllowAllPolicy();
        var call = new ToolCall("any", JsonDocument.Parse("{}").RootElement);
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>());

        var approved = policy.Approve(call, snap, out var reason);
        approved.Should().BeTrue();
        reason.Should().Be("OK");
    }
}

public class PerfHeadroomTests
{
    private static ToolCall Call(string id) =>
        new(id, JsonDocument.Parse("{}").RootElement);

    [Fact]
    public void Approve_returns_OK_when_no_elapsed_data()
    {
        var p = new PerfHeadroom(TimeSpan.FromSeconds(1));
        p.Approve(Call("t1"), new WorldSnapshot(0, new Dictionary<string, object?>()), out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_ignores_non_TimeSpan_elapsed_data()
    {
        var p = new PerfHeadroom(TimeSpan.FromSeconds(1));
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:t1"] = "not a TimeSpan",
        });
        p.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_accumulates_elapsed_and_rejects_after_budget_exceeded()
    {
        var p = new PerfHeadroom(TimeSpan.FromMilliseconds(100));

        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:t1"] = TimeSpan.FromMilliseconds(60),
        });

        p.Approve(Call("t1"), snap, out var first).Should().BeTrue();
        first.Should().Be("OK");

        p.Approve(Call("t1"), snap, out var second).Should().BeFalse();
        second.Should().Contain("exceeded time budget").And.Contain("t1");
    }

    [Fact]
    public void Approve_tracks_each_tool_independently_and_is_case_insensitive()
    {
        var p = new PerfHeadroom(TimeSpan.FromMilliseconds(50));

        var snapA = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:Alpha"] = TimeSpan.FromMilliseconds(40),
        });
        var snapB = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:beta"] = TimeSpan.FromMilliseconds(40),
        });

        p.Approve(new ToolCall("Alpha", JsonDocument.Parse("{}").RootElement), snapA, out _).Should().BeTrue();
        p.Approve(new ToolCall("beta", JsonDocument.Parse("{}").RootElement), snapB, out _).Should().BeTrue();

        var snapAOver = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:alpha"] = TimeSpan.FromMilliseconds(40),
        });
        p.Approve(new ToolCall("alpha", JsonDocument.Parse("{}").RootElement), snapAOver, out _).Should().BeFalse();
    }

    [Fact]
    public void Reset_clears_accumulated_state()
    {
        var p = new PerfHeadroom(TimeSpan.FromMilliseconds(100));
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:t1"] = TimeSpan.FromMilliseconds(60),
        });
        p.Approve(Call("t1"), snap, out _);
        p.Approve(Call("t1"), snap, out _).Should().BeFalse();

        p.Reset();
        p.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }
}

public class OutputPathSandboxedTests
{
    [Fact]
    public void Approve_returns_OK_when_no_OutputRoot_in_state()
    {
        var p = new OutputPathSandboxed();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
        var call = new ToolCall("t", JsonDocument.Parse("""{"output":"/x"}""").RootElement);
        p.Approve(call, snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_returns_OK_when_OutputRoot_is_not_string()
    {
        var p = new OutputPathSandboxed();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = 123 });
        var call = new ToolCall("t", JsonDocument.Parse("""{"output":"/x"}""").RootElement);
        p.Approve(call, snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_returns_OK_when_arguments_not_object()
    {
        var p = new OutputPathSandboxed();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = "/tmp" });
        var call = new ToolCall("t", JsonDocument.Parse("[]").RootElement);
        p.Approve(call, snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_returns_OK_when_arguments_have_no_output_property()
    {
        var p = new OutputPathSandboxed();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = "/tmp" });
        var call = new ToolCall("t", JsonDocument.Parse("""{"other":"x"}""").RootElement);
        p.Approve(call, snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_returns_OK_when_output_inside_sandbox()
    {
        var p = new OutputPathSandboxed();
        var tmp = Path.GetTempPath();
        var inside = Path.Combine(tmp, "inside.txt");
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = tmp });
        var call = new ToolCall("t", JsonDocument.Parse($$"""{"output":"{{inside.Replace("\\", "\\\\")}}"}""").RootElement);
        p.Approve(call, snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_rejects_path_outside_sandbox()
    {
        var p = new OutputPathSandboxed();
        var sandbox = Path.Combine(Path.GetTempPath(), "sandbox_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            var outsideAbsolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "outside_" + Guid.NewGuid().ToString("N")));
            var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = sandbox });
            var call = new ToolCall("t", JsonDocument.Parse($$"""{"output":"{{outsideAbsolute.Replace("\\", "\\\\")}}"}""").RootElement);
            p.Approve(call, snap, out var reason).Should().BeFalse();
            reason.Should().Contain("escapes sandbox");
        }
        finally
        {
            if (Directory.Exists(sandbox)) Directory.Delete(sandbox);
        }
    }

    [Fact]
    public void Approve_rejects_null_output_value_as_invalid_path()
    {
        var p = new OutputPathSandboxed();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = Path.GetTempPath() });
        var call = new ToolCall("t", JsonDocument.Parse("""{"output":null}""").RootElement);
        p.Approve(call, snap, out var reason).Should().BeFalse();
        reason.Should().Be("Invalid output path");
    }

    [Fact]
    public void Approve_rejects_invalid_path_with_invalid_chars()
    {
        var p = new OutputPathSandboxed();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = Path.GetTempPath() });
        var bad = "\0bad" + new string(Path.GetInvalidPathChars());
        var call = new ToolCall("t", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["output"] = bad })).RootElement);
        p.Approve(call, snap, out var reason).Should().BeFalse();
        reason.Should().Be("Invalid output path");
    }
}

using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Policies;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>Tests for output path sandboxed gap coverage.</summary>
public sealed class OutputPathSandboxedGapCoverageTests
{
    [Fact]
    public void Approve_allows_relative_output_inside_sandbox()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "ashlar-sandbox-gap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            var policy = new OutputPathSandboxed();
            var relativeOutput = Path.Combine("nested", "output.txt");
            var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = sandbox });
            var call = new ToolCall(
                "writer",
                JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["output"] = Path.Combine(sandbox, relativeOutput),
                })).RootElement);

            policy.Approve(call, snap, out var reason).Should().BeTrue();
            reason.Should().Be("OK");
        }
        finally
        {
            if (Directory.Exists(sandbox))
                Directory.Delete(sandbox, recursive: true);
        }
    }

    [Fact]
    public void Approve_allows_output_equal_to_sandbox_root()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "ashlar-sandbox-gap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            var policy = new OutputPathSandboxed();
            var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = sandbox });
            var call = new ToolCall(
                "writer",
                JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["output"] = sandbox,
                })).RootElement);

            policy.Approve(call, snap, out var reason).Should().BeTrue();
            reason.Should().Be("OK");
        }
        finally
        {
            if (Directory.Exists(sandbox))
                Directory.Delete(sandbox, recursive: true);
        }
    }

    [Fact]
    public void Approve_rejects_output_property_with_null_string_as_invalid_path()
    {
        var policy = new OutputPathSandboxed();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["OutputRoot"] = Path.GetTempPath() });
        var call = new ToolCall("writer", JsonDocument.Parse("""{"output":null}""").RootElement);

        policy.Approve(call, snap, out var reason).Should().BeFalse();
        reason.Should().Be("Invalid output path");
    }
}

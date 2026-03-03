using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Policies;

/// <summary>
/// Unit tests for MaxWriteSize policy — ensures repository file writes
/// do not exceed the configured size limit (default 200KB).
/// </summary>
public sealed class MaxWriteSizeTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    private static ToolCall CreateWriteCall(string content)
    {
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs", content });
        return new ToolCall("repo.fs.write", json);
    }

    [Fact]
    public void Approve_ContentUnderLimit_ReturnsTrue()
    {
        var policy = new MaxWriteSize(200_000);
        var content = new string('x', 100_000);
        var call = CreateWriteCall(content);

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_ContentAtLimit_ReturnsTrue()
    {
        var policy = new MaxWriteSize(200_000);
        var content = new string('x', 200_000);
        var call = CreateWriteCall(content);

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_ContentOverLimit_ReturnsFalse()
    {
        var policy = new MaxWriteSize(200_000);
        var content = new string('x', 200_001);
        var call = CreateWriteCall(content);

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeFalse();
        reason.Should().Contain("Write too large");
        reason.Should().Contain("200001");
        reason.Should().Contain("200000");
    }

    [Fact]
    public void Approve_ContentOverLimit_WithCustomLimit_ReturnsFalse()
    {
        var policy = new MaxWriteSize(1000);
        var content = new string('x', 1001);
        var call = CreateWriteCall(content);

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeFalse();
        reason.Should().Contain("Write too large");
        reason.Should().Contain("1001");
        reason.Should().Contain("1000");
    }

    [Fact]
    public void Approve_RepoFsSearchReplace_IgnoresSize_ReturnsTrue()
    {
        var policy = new MaxWriteSize(10);
        var content = new string('x', 1000);
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs", old = "a", new_ = content });
        var call = new ToolCall("repo.fs.search_replace", json);

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_ContentEmpty_ReturnsTrue()
    {
        var policy = new MaxWriteSize(200_000);
        var call = CreateWriteCall("");

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_OtherTool_ReturnsTrue()
    {
        var policy = new MaxWriteSize(10);
        var content = new string('x', 1000);
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs", content });
        var call = new ToolCall("other.tool", json);

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_ContentNull_ReturnsFalse()
    {
        var policy = new MaxWriteSize(200_000);
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs", content = (string?)null });
        var call = new ToolCall("repo.fs.write", json);

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeFalse();
        reason.Should().Contain("null");
    }

    [Fact]
    public void Approve_ContentOneByte_ReturnsTrue()
    {
        var policy = new MaxWriteSize(200_000);
        var call = CreateWriteCall("x");

        var result = policy.Approve(call, EmptySnapshot, out var reason);

        result.Should().BeTrue();
        reason.Should().Be("OK");
    }
}

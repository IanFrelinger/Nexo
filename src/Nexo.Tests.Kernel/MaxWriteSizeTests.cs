using System.Text.Json;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Kernel;

public class MaxWriteSizeTests
{
    [Fact]
    public void Allows_non_write_calls()
    {
        var p = new MaxWriteSize(100);
        p.Approve(new ToolCall("dotnet.build", JsonDocument.Parse("{}").RootElement),
            new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Fact]
    public void Allows_write_when_arguments_not_object()
    {
        var p = new MaxWriteSize(100);
        p.Approve(new ToolCall("repo.fs.write", JsonDocument.Parse("[]").RootElement),
            new WorldSnapshot(0, new Dictionary<string, object?>()), out _).Should().BeTrue();
    }

    [Fact]
    public void Rejects_write_missing_content()
    {
        var p = new MaxWriteSize(100);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"path":"x"}""").RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeFalse();
        r.Should().Contain("content is required");
    }

    [Fact]
    public void Rejects_write_null_content()
    {
        var p = new MaxWriteSize(100);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"content":null}""").RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeFalse();
        r.Should().Contain("cannot be null");
    }

    [Fact]
    public void Rejects_write_exceeding_size()
    {
        var p = new MaxWriteSize(5);
        var big = new string('a', 10);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["content"] = big })).RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeFalse();
        r.Should().Contain("Write too large");
    }

    [Fact]
    public void Allows_write_within_size()
    {
        var p = new MaxWriteSize(100);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"content":"hi"}""").RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Fact]
    public void Default_max_size_is_200KB()
    {
        var p = new MaxWriteSize();
        var json = JsonSerializer.Serialize(new Dictionary<string, string> { ["content"] = new string('x', 200_000) });
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse(json).RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out _).Should().BeTrue();
    }
}

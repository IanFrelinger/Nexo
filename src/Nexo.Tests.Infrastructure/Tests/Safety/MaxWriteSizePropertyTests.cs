using System.Text.Json;
using CsCheck;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Safety;

/// <summary>
/// Property-based tests for MaxWriteSize. Proves safety invariants hold for arbitrary inputs.
/// </summary>
[Trait("Category", "Safety")]
[Trait("Category", "Unit")]
public sealed class MaxWriteSizePropertyTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());
    private readonly MaxWriteSize _maxWriteSize = new(200_000);

    private static ToolCall CreateWriteCall(string content)
    {
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs", content });
        return new ToolCall("repo.fs.write", json);
    }

    [Fact]
    public void MaxWriteSize_NeverAllows_ContentAboveLimit()
    {
        Gen.Int[200 * 1024 + 1, 10 * 1024 * 1024]
            .Select(size => new string('x', size))
            .Sample(content =>
            {
                var call = CreateWriteCall(content);
                var result = _maxWriteSize.Approve(call, EmptySnapshot, out var reason);

                result.Should().BeFalse("no content above 200KB is ever allowed");
                reason.Should().Contain("Write too large");
            });
    }

    [Fact]
    public void MaxWriteSize_AtExactLimit_IsAllowed()
    {
        var content = new string('x', 200_000);
        var call = CreateWriteCall(content);
        var result = _maxWriteSize.Approve(call, EmptySnapshot, out var reason);
        result.Should().BeTrue("content at exactly 200KB must be allowed");
        reason.Should().Be("OK");
    }

    [Fact]
    public void MaxWriteSize_AlwaysAllows_ContentAtOrBelowLimit()
    {
        Gen.Int[0, 199_999]
            .Select(size => new string('x', size))
            .Sample(content =>
            {
                var call = CreateWriteCall(content);
                var result = _maxWriteSize.Approve(call, EmptySnapshot, out var reason);

                result.Should().BeTrue("content at or below limit must be allowed");
                reason.Should().Be("OK");
            });
    }

    [Fact]
    public void MaxWriteSize_NeverThrows_ForAnyInput()
    {
        Gen.Int[0, 2_000_000]
            .Select(n => new string('x', n))
            .Sample(content =>
            {
                var call = CreateWriteCall(content);
                var act = () => _maxWriteSize.Approve(call, EmptySnapshot, out _);
                act.Should().NotThrow();
            });
    }

    [Fact]
    public void MaxWriteSize_NeverThrows_ForNullContent()
    {
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs", content = (string?)null });
        var call = new ToolCall("repo.fs.write", json);

        var act = () => _maxWriteSize.Approve(call, EmptySnapshot, out _);
        act.Should().NotThrow();
    }
}

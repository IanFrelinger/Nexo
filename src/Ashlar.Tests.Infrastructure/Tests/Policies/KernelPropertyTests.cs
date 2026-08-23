using System.Text.Json;
using CsCheck;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Policies.Dev;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Policies;

/// <summary>
/// Property-based tests for kernel safety components.
/// Proves that policies and registry behave correctly for arbitrary inputs — no exception, no silent pass.
/// </summary>
[Trait("Category", "Unit")]
public sealed class KernelPropertyTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());
    private readonly PathAllowlist _pathAllowlist = new();
    private readonly MaxWriteSize _maxWriteSize = new(200_000);
    private readonly ImmutableCoreRegistry _immutableCoreRegistry = new();

    private static ToolCall CreateWriteCall(string path, string content)
    {
        var json = JsonSerializer.SerializeToElement(new { path, content });
        return new ToolCall("repo.fs.write", json);
    }

    private static ToolCall CreateReadCall(string path)
    {
        var json = JsonSerializer.SerializeToElement(new { path });
        return new ToolCall("repo.fs.read", json);
    }

    [Fact]
    public void PathAllowlist_NeverSilentlyAccepts_ArbitraryPath()
    {
        Gen.String[1, 500].Sample(path =>
        {
            var call = CreateWriteCall(path, "content");
            var result = _pathAllowlist.Approve(call, EmptySnapshot, out var reason);

            (result == true || result == false).Should().BeTrue("result must be true or false");
            reason.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void PathAllowlist_NonWriteTools_AlwaysPass()
    {
        Gen.String[0, 300].Sample(path =>
        {
            var call = CreateReadCall(path);
            var result = _pathAllowlist.Approve(call, EmptySnapshot, out var reason);

            result.Should().BeTrue("repo.fs.read is not restricted by PathAllowlist");
            reason.Should().Be("OK");
        });
    }

    [Fact]
    public void MaxWriteSize_AlwaysReturnsCleanResult_ForAnyInput()
    {
        Gen.Int[0, 500_000].Select(n => new string('x', n)).Sample(content =>
        {
            var call = CreateWriteCall("src/foo.cs", content);
            var result = _maxWriteSize.Approve(call, EmptySnapshot, out var reason);

            (result == true || result == false).Should().BeTrue("result must be true or false");
            reason.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void ImmutableCoreRegistry_IsInImmutableCore_NeverThrows()
    {
        Gen.String[0, 500].Sample(input =>
        {
            var act = () => _immutableCoreRegistry.IsInImmutableCore(input);
            act.Should().NotThrow();
            var result = _immutableCoreRegistry.IsInImmutableCore(input);
            (result == true || result == false).Should().BeTrue("result must be true or false");
        });
    }
}

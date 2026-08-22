using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Policies;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>Tests for allow all policy.</summary>
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

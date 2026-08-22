using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Policies.Dev;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Policies;

/// <summary>Tests for max write size gap coverage.</summary>
[Trait("Category", "Unit")]
public sealed class MaxWriteSizeGapCoverageTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    [Fact]
    public void Approve_missing_content_property_returns_false()
    {
        var policy = new MaxWriteSize(200_000);
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs" });
        var call = new ToolCall("repo.fs.write", json);

        policy.Approve(call, EmptySnapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("content is required");
    }

    [Fact]
    public void Approve_significantly_over_limit_includes_sizes_in_reason()
    {
        var policy = new MaxWriteSize(200_000);
        var content = new string('x', 500_000);
        var json = JsonSerializer.SerializeToElement(new { path = "src/foo.cs", content });
        var call = new ToolCall("repo.fs.write", json);

        policy.Approve(call, EmptySnapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("500000");
        reason.Should().Contain("200000");
    }

    [Fact]
    public void Approve_non_object_arguments_returns_true()
    {
        var policy = new MaxWriteSize(10);
        var call = new ToolCall("repo.fs.write", JsonSerializer.SerializeToElement("not-an-object"));

        policy.Approve(call, EmptySnapshot, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }
}

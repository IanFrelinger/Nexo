using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Abstractions.Database;
using Nexo.Abstractions.Transport;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for barrier context.</summary>
public class BarrierContextTests
{
    [Fact]
    public void Create_validates_level_and_sets_fields()
    {
        var h = new BarrierHierarchy(new[] { new BarrierLevel("public", 0) });
        var ctx = BarrierContext.Create("public", "jwt", "user", "corr", h, "detail");
        ctx.Level.Should().Be("public");
        ctx.AuthoritySource.Should().Be("jwt");
        ctx.IssuedTo.Should().Be("user");
        ctx.CorrelationId.Should().Be("corr");
        ctx.ResolutionDetail.Should().Be("detail");
        ctx.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_rejects_unknown_level_and_null_hierarchy()
    {
        var h = new BarrierHierarchy(new[] { new BarrierLevel("public", 0) });
        Assert.Throws<ArgumentNullException>(() =>
            BarrierContext.Create("public", "jwt", "user", "corr", null!));
        Assert.Throws<ArgumentException>(() =>
            BarrierContext.Create("secret", "jwt", "user", "corr", h));
    }

    [Fact]
    public void ForAgent_updates_issued_to()
    {
        var h = new BarrierHierarchy(new[] { new BarrierLevel("public", 0) });
        var ctx = BarrierContext.Create("public", "jwt", "user", "corr", h);
        ctx.ForAgent("agent-1").IssuedTo.Should().Be("agent-1");
    }
}

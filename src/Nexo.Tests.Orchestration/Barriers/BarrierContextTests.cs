using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Xunit;

namespace Nexo.Tests.Orchestration.Barriers;

/// <summary>Tests for barrier context.</summary>
public sealed class BarrierContextTests
{
    [Fact]
    public void Create_UnknownLevel_ThrowsWithLevelName()
    {
        var hierarchy = new BarrierHierarchy([
            new BarrierLevel("low", 0),
            new BarrierLevel("high", 1)
        ]);

        var act = () => BarrierContext.Create(
            "unknown",
            BarrierAuthoritySource.Cli,
            "*",
            "corr-1",
            hierarchy);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*unknown*");
    }

    [Fact]
    public void Create_KnownLevel_SetsAllFields()
    {
        var hierarchy = new BarrierHierarchy([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1)
        ]);

        var context = BarrierContext.Create(
            "internal",
            BarrierAuthoritySource.Cli,
            "*",
            "corr-1",
            hierarchy);

        context.Level.Should().Be("internal");
        context.AuthoritySource.Should().Be(BarrierAuthoritySource.Cli);
        context.IssuedTo.Should().Be("*");
        context.CorrelationId.Should().Be("corr-1");
        context.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ForAgent_UpdatesIssuedTo_Only()
    {
        var hierarchy = new BarrierHierarchy([
            new BarrierLevel("public", 0)
        ]);
        var context = BarrierContext.Create(
            "public",
            BarrierAuthoritySource.Header,
            "*",
            "corr-1",
            hierarchy);

        var perAgent = context.ForAgent("agent-a");

        perAgent.IssuedTo.Should().Be("agent-a");
        perAgent.Level.Should().Be(context.Level);
        perAgent.AuthoritySource.Should().Be(context.AuthoritySource);
        perAgent.CorrelationId.Should().Be(context.CorrelationId);
        perAgent.IssuedAt.Should().Be(context.IssuedAt);
    }

    [Fact]
    public void Context_IsImmutable_WithCreatesNewInstance()
    {
        var hierarchy = new BarrierHierarchy([
            new BarrierLevel("public", 0)
        ]);
        var context = BarrierContext.Create(
            "public",
            BarrierAuthoritySource.Cli,
            "*",
            "corr-1",
            hierarchy);

        var modified = context with { IssuedTo = "agent-a" };

        modified.Should().NotBeSameAs(context);
        context.IssuedTo.Should().Be("*");
        modified.IssuedTo.Should().Be("agent-a");
    }
}

using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for barrier context gap coverage.</summary>
public sealed class BarrierContextGapCoverageTests
{
    [Fact]
    public void Create_normalizes_null_optional_strings()
    {
        var hierarchy = CreateHierarchy();
        var context = BarrierContext.Create(
            "public",
            authoritySource: null!,
            issuedTo: null!,
            correlationId: null!,
            hierarchy);

        context.AuthoritySource.Should().BeEmpty();
        context.IssuedTo.Should().BeEmpty();
        context.CorrelationId.Should().BeEmpty();
    }

    [Fact]
    public void ForAgent_returns_copy_with_updated_issued_to()
    {
        var hierarchy = CreateHierarchy();
        var context = BarrierContext.Create(
            "internal",
            BarrierAuthoritySource.JwtClaim,
            issuedTo: "original",
            correlationId: "corr-for-agent",
            hierarchy);

        var updated = context.ForAgent("agent-42");

        updated.IssuedTo.Should().Be("agent-42");
        context.IssuedTo.Should().Be("original");
        updated.Level.Should().Be(context.Level);
    }

    [Fact]
    public void Create_throws_for_unknown_level()
    {
        var hierarchy = CreateHierarchy();
        var act = () => BarrierContext.Create(
            "top-secret",
            BarrierAuthoritySource.Header,
            issuedTo: "user",
            correlationId: "corr",
            hierarchy);

        act.Should().Throw<ArgumentException>().WithParameterName("level");
    }

    [Fact]
    public void Create_throws_for_null_hierarchy()
    {
        var act = () => BarrierContext.Create(
            "public",
            BarrierAuthoritySource.Header,
            issuedTo: "user",
            correlationId: "corr",
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("hierarchy");
    }

    [Fact]
    public void Create_preserves_resolution_detail()
    {
        var hierarchy = CreateHierarchy();
        var context = BarrierContext.Create(
            "public",
            BarrierAuthoritySource.ApiKey,
            issuedTo: "svc",
            correlationId: "corr-detail",
            hierarchy,
            resolutionDetail: "matched api key hash");

        context.ResolutionDetail.Should().Be("matched api key hash");
        context.AuthoritySource.Should().Be(BarrierAuthoritySource.ApiKey);
    }

    [Fact]
    public void ForAgent_preserves_resolution_detail_and_correlation_id()
    {
        var hierarchy = CreateHierarchy();
        var context = BarrierContext.Create(
            "internal",
            BarrierAuthoritySource.JwtClaim,
            issuedTo: "original",
            correlationId: "corr-preserve",
            hierarchy,
            resolutionDetail: "claim match");

        var updated = context.ForAgent("agent-99");

        updated.ResolutionDetail.Should().Be("claim match");
        updated.CorrelationId.Should().Be("corr-preserve");
    }

    private static BarrierHierarchy CreateHierarchy()
        => new([
            new BarrierLevel("public", 0),
            new BarrierLevel("internal", 1),
        ]);
}

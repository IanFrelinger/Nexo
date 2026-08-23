using FluentAssertions;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Barriers.Identity;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for barrier abstractions gap coverage.</summary>
public sealed class BarrierAbstractionsGapCoverageTests
{
    [Fact]
    public void BarrierAuditEventType_exposes_expected_constants()
    {
        BarrierAuditEventType.ContextInitialized.Should().Be("BARRIER_INITIALIZED");
        BarrierAuditEventType.AgentInvoked.Should().Be("BARRIER_AGENT_INVOKED");
        BarrierAuditEventType.CeilingExceeded.Should().Be("BARRIER_CEILING_EXCEEDED");
        BarrierAuditEventType.IdentityResolved.Should().Be("BARRIER_IDENTITY_RESOLVED");
    }

    [Fact]
    public void BarrierAuthoritySource_exposes_expected_labels()
    {
        BarrierAuthoritySource.Cli.Should().Be("CLI");
        BarrierAuthoritySource.Header.Should().Be("Header");
        BarrierAuthoritySource.JwtClaim.Should().Be("JwtClaim");
        BarrierAuthoritySource.ApiKey.Should().Be("ApiKey");
        BarrierAuthoritySource.PkiCertificate.Should().Be("PkiCertificate");
    }

    [Fact]
    public void BarrierAuditEvent_supports_optional_detail()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var evt = new BarrierAuditEvent(
            BarrierAuditEventType.ValidationFailed,
            "internal",
            BarrierAuthoritySource.Claim,
            AgentName: "agent-1",
            CorrelationId: "corr-1",
            SpanId: "span-1",
            occurredAt,
            Detail: "validation detail");

        evt.EventType.Should().Be(BarrierAuditEventType.ValidationFailed);
        evt.Detail.Should().Be("validation detail");
        evt.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void BarrierResolutionResult_defaults_detail_to_null()
    {
        var result = new BarrierResolutionResult(
            "public",
            "TestResolver",
            BarrierAuthoritySource.Header);

        result.ResolvedLevel.Should().Be("public");
        result.ResolverName.Should().Be("TestResolver");
        result.Detail.Should().BeNull();
    }

    [Fact]
    public void BarrierOptions_defaults_levels_and_flags()
    {
        var options = new BarrierOptions();

        options.Levels.Should().BeEmpty();
        options.RequireExplicitBarrier.Should().BeFalse();
        options.HostCeiling.Should().BeNull();
    }
}

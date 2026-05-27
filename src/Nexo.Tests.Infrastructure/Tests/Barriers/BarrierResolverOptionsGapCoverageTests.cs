using FluentAssertions;
using Nexo.Runtime.Barriers.Identity;
using Nexo.Runtime.Barriers.Identity.Resolvers;
using Nexo.Runtime.Barriers.Sinks;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Barriers;

public sealed class BarrierResolverOptionsGapCoverageTests
{
    [Fact]
    public void ApiKeyResolverOptions_has_expected_defaults()
    {
        var options = new ApiKeyResolverOptions();

        options.HeaderName.Should().Be("x-nexo-api-key");
        options.KeyMapping.Should().BeEmpty();
    }

    [Fact]
    public void JwtClaimResolverOptions_has_expected_defaults()
    {
        var options = new JwtClaimResolverOptions();

        options.ClaimName.Should().Be("nexo_barrier");
        options.ClaimValueMapping.Should().BeEmpty();
    }

    [Fact]
    public void PkiCertificateResolverOptions_starts_with_empty_rules()
    {
        var options = new PkiCertificateResolverOptions();

        options.Rules.Should().BeEmpty();
    }

    [Fact]
    public void CertificateBarrierRule_exposes_match_fields()
    {
        var rule = new CertificateBarrierRule
        {
            Name = "agent-cert",
            MatchField = "san",
            MatchPattern = "*.agent.local",
            BarrierLevel = "internal",
        };

        rule.Name.Should().Be("agent-cert");
        rule.MatchField.Should().Be("san");
        rule.MatchPattern.Should().Be("*.agent.local");
        rule.BarrierLevel.Should().Be("internal");
    }
}

public sealed class FileBarrierAuditSinkOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_audit_sink_configuration()
    {
        var options = new FileBarrierAuditSinkOptions();

        options.Directory.Should().Be("audit");
        options.FilePrefix.Should().Be("audit-barriers");
        options.MaxFileSizeBytes.Should().Be(10 * 1024 * 1024);
        options.MaxRotatedFiles.Should().Be(10);
        options.FlushIntervalMs.Should().Be(5_000);
        options.FlushEveryEvent.Should().BeFalse();
        options.ChannelCapacity.Should().Be(4096);
    }
}

public sealed class BarrierIdentityResolverOptionsGapCoverageTests
{
    [Fact]
    public void ResolverPriority_starts_empty()
    {
        new BarrierIdentityResolverOptions().ResolverPriority.Should().BeEmpty();
    }
}

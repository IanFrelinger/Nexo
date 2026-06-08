using FluentAssertions;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

public sealed class MeshFleetTrustPolicyGapCoverageTests
{
    [Theory]
    [InlineData(null, "any")]
    [InlineData("", "any")]
    [InlineData("   ", "any")]
    [InlineData("TRUSTED-ONLY", "trusted-only")]
    [InlineData("trusted_only", "trusted-only")]
    [InlineData("Trusted-Preferred", "trusted-preferred")]
    [InlineData("trusted_preferred", "trusted-preferred")]
    [InlineData("Allowlist", "allowlist")]
    [InlineData("unknown-policy", "any")]
    public void NormalizePolicy_maps_aliases_and_defaults(string? input, string expected)
    {
        MeshFleetTrustPolicy.NormalizePolicy(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(MeshFleetTrustTier.Trusted, "trusted-only", true)]
    [InlineData(MeshFleetTrustTier.Unknown, "trusted-only", false)]
    [InlineData(MeshFleetTrustTier.Untrusted, "trusted-only", false)]
    [InlineData(MeshFleetTrustTier.Trusted, "allowlist", true)]
    [InlineData(MeshFleetTrustTier.Unknown, "allowlist", false)]
    [InlineData(MeshFleetTrustTier.Trusted, "trusted-preferred", true)]
    [InlineData(MeshFleetTrustTier.Unknown, "trusted-preferred", true)]
    [InlineData(MeshFleetTrustTier.Untrusted, "trusted-preferred", false)]
    [InlineData(MeshFleetTrustTier.Untrusted, "any", true)]
    [InlineData(MeshFleetTrustTier.Unknown, "any", true)]
    public void IsEligible_applies_normalized_policy_rules(
        MeshFleetTrustTier tier,
        string policy,
        bool expected)
    {
        MeshFleetTrustPolicy.IsEligible(tier, policy).Should().Be(expected);
    }
}

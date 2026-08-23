using FluentAssertions;
using Ashlar.Core.Application.Autonomy;
using Xunit;

namespace Ashlar.Tests.Application.Tests.Autonomy;

/// <summary>
/// Tier classification (autonomous self-extension spec §3): a pure function of the
/// declared touch-set — blast radius, never confidence (I-2) — fail-closed on undeclared
/// sets (R1.4), with Tier 2 gating the objective's ORIGIN for authority-rule changes and
/// the kernel enumeration containing its own enforcement code (R3.3).
/// </summary>
public sealed class ObjectiveTierClassifierTests
{
    [Fact]
    public void DeclaredLeafTouchSet_IsTier0_AndAnySourceMayOpenASession()
    {
        var touch = new TouchSet
        {
            PathPrefixes = new[] { "src/Ashlar.Bricks.Weather/" },
            Namespaces = new[] { "Ashlar.Bricks.Weather" },
            Capabilities = new[] { "repo.fs.write", "dotnet.build" },
        };

        var verdict = ObjectiveTierClassifier.Classify(touch);

        verdict.Tier.Should().Be(ObjectiveTier.Tier0Autonomous);
        verdict.MaySessionOpen(ObjectiveSource.Telemetry).Should().BeTrue();
    }

    [Theory]
    [InlineData("src/Ashlar.Infrastructure/Certification/HotSwap/")]
    [InlineData("src/Ashlar.Analyzers/")]
    [InlineData("src/Ashlar.Policies.Dev/")]
    [InlineData("scripts/ci/")]
    public void KernelPaths_ClassifyTier1_WithTheHitNamed(string kernelPath)
    {
        var verdict = ObjectiveTierClassifier.Classify(new TouchSet
        {
            PathPrefixes = new[] { "src/Ashlar.Bricks.Weather/", kernelPath },
        });

        verdict.Tier.Should().Be(ObjectiveTier.Tier1HumanAdmission);
        verdict.Reasons.Should().Contain(r => r.Contains(kernelPath));
    }

    [Fact]
    public void KernelNamespaces_ClassifyTier1_EvenWithoutKernelPaths()
    {
        var verdict = ObjectiveTierClassifier.Classify(new TouchSet
        {
            Namespaces = new[] { "Ashlar.Infrastructure.Certification.HotSwap" },
        });

        verdict.Tier.Should().Be(ObjectiveTier.Tier1HumanAdmission);
    }

    [Fact]
    public void AuthorityRulePaths_ClassifyTier2_AndMachineSourcesMayNotOpenASession()
    {
        var verdict = ObjectiveTierClassifier.Classify(new TouchSet
        {
            PathPrefixes = new[] { "src/Ashlar.Core.Application/Autonomy/" },
        });

        verdict.Tier.Should().Be(ObjectiveTier.Tier2HumanObjective);
        verdict.MaySessionOpen(ObjectiveSource.Human).Should().BeTrue();
        verdict.MaySessionOpen(ObjectiveSource.Triage).Should().BeFalse(
            "machine sources must not even open a proposal session against authority rules (R3.1)");
        verdict.MaySessionOpen(ObjectiveSource.Telemetry).Should().BeFalse();
    }

    [Fact]
    public void UndeclaredTouchSet_IsTier1_FailClosed()
    {
        var verdict = ObjectiveTierClassifier.Classify(new TouchSet());

        verdict.Tier.Should().Be(ObjectiveTier.Tier1HumanAdmission);
        verdict.Reasons.Should().ContainSingle().Which.Should().Contain("R1.4");
    }

    [Fact]
    public void TheKernelEnumeration_ContainsItsOwnEnforcementCode()
    {
        // R3.3 structurally: widening Tier 0 means editing the kernel/authority lists,
        // which live in a path that is itself kernel AND authority-rule — so that edit is
        // a Tier 2 change by construction, not by convention.
        TrustKernel.IsKernelPath("src/Ashlar.Core.Application/Autonomy/TrustKernel.cs").Should().BeTrue();
        TrustKernel.IsAuthorityRulePath("src/Ashlar.Core.Application/Autonomy/TrustKernel.cs").Should().BeTrue();
        TrustKernel.IsKernelNamespace("Ashlar.Core.Application.Autonomy").Should().BeTrue();
    }

    [Fact]
    public void TouchSet_DerivesConfinement_FromTheSameDeclaration()
    {
        var touch = new TouchSet { PathPrefixes = new[] { "src/Ashlar.Bricks.Weather/" } };

        touch.ToProposerConfinement().ToPathAllowlistPrefixes()
            .Should().ContainSingle("the tier classifier and the sandbox confinement must read one declaration")
            .Which.Should().Be("src/Ashlar.Bricks.Weather/");
    }

    [Fact]
    public void TouchSet_Normalization_RefusesDirtyDeclarations()
    {
        var traversal = () => new TouchSet { PathPrefixes = new[] { "../escape/" } }.Normalize();
        var badNamespace = () => new TouchSet { Namespaces = new[] { "Ashlar;.Evil" } }.Normalize();

        traversal.Should().Throw<InvalidOperationException>();
        badNamespace.Should().Throw<InvalidOperationException>().WithMessage("*dotted identifier*");
    }
}

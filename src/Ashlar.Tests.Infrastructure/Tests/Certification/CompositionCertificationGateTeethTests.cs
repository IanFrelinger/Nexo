using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for composition certification gate teeth.</summary>
[Trait("Category", "Certification")]
public sealed class CompositionCertificationGateTeethTests
{
    [Fact]
    public async Task CorrectComposition_StrongWitness_Admits_WithZeroEscapeRate()
    {
        var ctx = await CompositionProbeFixtures.CreateAdmittedContextAsync();
        var decision = await ctx.CompositionAdmission.CertifyAndAdmitAsync(new CompositionCertificationRequest
        {
            Spec = CompositionProbeFixtures.CorrectSpec(),
            Witness = CompositionProbeFixtures.StrongWitness(),
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeTrue(
            $"expected ADMIT, got {decision.FailureCheck}: {decision.Record.Reason}");
        decision.Record.CompositionEscapeRate.Should().Be(0);
        decision.Record.Signed.Should().BeTrue();
        ctx.CompositionSigner.Verify(decision.Record).Should().BeTrue();
        ctx.CompositionRegistry.GetComposition(CompositionProbeFixtures.CompositionId).Should().NotBeNull();
    }

    [Fact]
    public async Task BrokenComposition_StrongWitness_Rejects()
    {
        var ctx = await CompositionProbeFixtures.CreateAdmittedContextAsync();
        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionProbeFixtures.BrokenSeamSpec(),
            Witness = CompositionProbeFixtures.StrongWitness(),
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse("broken composition must not admit");
        decision.FailureCheck.Should().BeOneOf("seam", "correctness");
    }

    [Fact]
    public async Task CorrectComposition_WeakWitness_Rejects_WithStructuralTeeth()
    {
        var ctx = await CompositionProbeFixtures.CreateAdmittedContextAsync();
        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionProbeFixtures.CorrectSpec(),
            Witness = CompositionProbeFixtures.WeakWitness(),
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse("weak witness must not admit when graph mutants survive");
        decision.FailureCheck.Should().Be("mutation");
        decision.Record.CompositionEscapeRate.Should().BeGreaterThan(0);
        decision.Record.SurvivingStructuralMutantIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UncertifiedConstituent_Rejects_Constituents()
    {
        var ctx = await CompositionProbeFixtures.CreateAdmittedContextAsync();
        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionProbeFixtures.UncertifiedConstituentSpec(),
            Witness = CompositionProbeFixtures.StrongWitness(),
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("constituents");
    }

    [Fact]
    public async Task NondeterministicComposition_Rejects_Determinism()
    {
        var ctx = await CompositionProbeFixtures.CreateAdmittedContextAsync();
        CompositionProbeFixtures.SeedSyntheticConstituent(
            ctx,
            new NondeterministicFormatterBrick(),
            "nondeterministic-formatter");

        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionProbeFixtures.NondeterministicSpec(),
            Witness = CompositionProbeFixtures.StrongWitness(),
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("determinism");
    }
}

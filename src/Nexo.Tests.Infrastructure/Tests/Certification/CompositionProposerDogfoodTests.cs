using FluentAssertions;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.Composition;
using Nexo.Tests.Infrastructure.Certification.Dogfood;
using Nexo.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CompositionProposerDogfoodTests
{
    [Fact]
    public async Task CorrectProposal_StrongWitness_Admits_WithZeroEscapeRate()
    {
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();
        var proposerInput = CompositionDogfoodHarness.BuildProposerInput(ctx);
        var service = CreateProposeCertifyService(ctx, ControlledCompositionProposer.CorrectVariant);

        var result = await service.ProposeCertifyAndAdmitAsync(
            proposerInput,
            witness,
            wiringMetadata: "composition-wiring-v0");

        result.Decision.Admitted.Should().BeTrue(
            $"expected ADMIT, got {result.Decision.FailureCheck}: {result.Decision.Record.Reason}");
        result.Decision.Record.CompositionEscapeRate.Should().Be(0);
        result.Decision.Record.Signed.Should().BeTrue();
        result.Proposal.Provenance.Should().StartWith("controlled-composition:correct");
        ctx.CompositionSigner.Verify(result.Decision.Record).Should().BeTrue();
    }

    [Fact]
    public async Task CorrectProposal_MatchesHandAuthoredCompositionResult()
    {
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();

        var handDecision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionDogfoodFixtures.HonestSpec(),
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        var proposerInput = CompositionDogfoodHarness.BuildProposerInput(ctx);
        var service = CreateProposeCertifyService(ctx, ControlledCompositionProposer.CorrectVariant);
        var proposed = await service.ProposeCertifyAndAdmitAsync(
            proposerInput,
            witness,
            wiringMetadata: "composition-wiring-v0");

        handDecision.Admitted.Should().BeTrue();
        proposed.Decision.Admitted.Should().BeTrue();
        proposed.Decision.Record.CompositionEscapeRate.Should().Be(handDecision.Record.CompositionEscapeRate);
        proposed.Decision.Record.Signed.Should().Be(handDecision.Record.Signed);
        proposed.Decision.Record.CompositionId.Should().Be(handDecision.Record.CompositionId);
    }

    [Theory]
    [InlineData(ControlledCompositionProposer.ReorderedWiringVariant, "correctness", "mutation", "seam")]
    [InlineData(ControlledCompositionProposer.DroppedBrickVariant, "correctness", "mutation", "seam")]
    [InlineData(ControlledCompositionProposer.TypeMismatchEdgeVariant, "seam", "correctness")]
    [InlineData(ControlledCompositionProposer.HallucinatedDependencyVariant, "constituents")]
    [InlineData(ControlledCompositionProposer.UncertifiedConstituentVariant, "constituents")]
    public async Task BadProposalVariants_AreRejectedByExistingGate(
        string variant,
        params string[] expectedFailureChecks)
    {
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();
        var proposerInput = CompositionDogfoodHarness.BuildProposerInput(ctx);
        var proposer = new ControlledCompositionProposer { Variant = variant };
        var proposal = await proposer.ProposeAsync(proposerInput);

        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = proposal.Spec with { CompositionId = witness.CompositionId },
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse($"bad proposal variant '{variant}' must not admit");
        decision.FailureCheck.Should().BeOneOf(expectedFailureChecks,
            $"variant '{variant}' must fail via existing gate checks, not agent bypass");
    }

    [Fact]
    public async Task TamperedConstituentCert_RejectedByConstituentIntegrity()
    {
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();

        ctx.BrickStore.Save(new CertificationRecord
        {
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = DamageHealthCompositionProposals.UncertifiedBrickId,
            ContentHash = "tampered",
            Signature = Convert.ToBase64String(new byte[32])
        });

        var proposerInput = CompositionDogfoodHarness.BuildProposerInput(ctx);
        var proposer = new ControlledCompositionProposer
        {
            Variant = ControlledCompositionProposer.UncertifiedConstituentVariant
        };
        var proposal = await proposer.ProposeAsync(proposerInput);

        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = proposal.Spec with { CompositionId = witness.CompositionId },
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("constituents");
    }

    private static ProposeAndCertifyCompositionService CreateProposeCertifyService(
        CertifiedCompositionTestContext ctx,
        string variant) =>
        new(
            new ControlledCompositionProposer { Variant = variant },
            ctx.CompositionAdmission);
}

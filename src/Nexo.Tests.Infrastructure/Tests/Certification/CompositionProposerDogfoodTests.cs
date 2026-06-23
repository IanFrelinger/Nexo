using FluentAssertions;
using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.Composition;
using Nexo.Tests.Infrastructure.Certification.Dogfood;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CompositionProposerDogfoodTests
{
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
}

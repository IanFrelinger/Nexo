using FluentAssertions;
using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Certification.Composition;
using Nexo.Tests.Infrastructure.Certification.Dogfood;
using Nexo.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class RealModelCompositionProposerDogfoodTests
{
  private static readonly string RecordedProposalPath = Path.Combine(
        AppContext.BaseDirectory,
        "Certification", "Dogfood", "RecordedCompositionProposals",
        "damage-to-health-pipeline.json");

    [Fact]
    public async Task RecordedRealProposal_TraversesIdenticalLoop_ReportsHonestGateVerdict()
    {
        var recording = RecordedCompositionProposal.FromJson(File.ReadAllText(RecordedProposalPath));
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();
        var proposerInput = CompositionDogfoodHarness.BuildProposerInput(ctx);

        var replayModel = RecordedCompositionGeneratorModel.FromJsonFiles(RecordedProposalPath);
        var service = new ProposeAndCertifyCompositionService(
            new ModelCompositionProposer(replayModel),
            ctx.CompositionAdmission);

        var result = await service.ProposeCertifyAndAdmitAsync(
            proposerInput,
            witness,
            wiringMetadata: "composition-wiring-v0");

        result.Proposal.Provenance.Should().Be(recording.Provenance);
        result.Proposal.Provenance.Should().StartWith("model:");
        result.Proposal.Provenance.Should().EndWith(":isolation-enforced");

        if (recording.GateVerdictAtRecording == "ADMIT")
        {
            result.Decision.Admitted.Should().BeTrue(
                $"recorded proposal certified first-try at recording; gate must ADMIT, got {result.Decision.FailureCheck}: {result.Decision.Record.Reason}");
            result.Decision.Record.CompositionEscapeRate.Should().Be(0);
            result.Decision.Record.Signed.Should().BeTrue();
            recording.FirstTryCertified.Should().BeTrue("recording metadata must honestly report first-try ADMIT");
        }
        else
        {
            result.Decision.Admitted.Should().BeFalse(
                "recorded proposal was REJECT at recording time; gate must not admit on replay");
            result.Decision.FailureCheck.Should().Be(recording.GateFailureCheckAtRecording);
            recording.FirstTryCertified.Should().BeFalse("recording metadata must honestly report first-try REJECT");
        }
    }

    [Fact]
    public async Task RecordedRealProposal_WhenAdmitted_MatchesHandAuthoredCompositionResult()
    {
        var recording = RecordedCompositionProposal.FromJson(File.ReadAllText(RecordedProposalPath));
        if (recording.GateVerdictAtRecording != "ADMIT")
        {
            // Honest skip: model's actual proposal did not certify — document, do not re-roll.
            return;
        }

        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();

        var handDecision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionDogfoodFixtures.HonestSpec(),
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        var replayModel = RecordedCompositionGeneratorModel.FromJsonFiles(RecordedProposalPath);
        var service = new ProposeAndCertifyCompositionService(
            new ModelCompositionProposer(replayModel),
            ctx.CompositionAdmission);

        var proposed = await service.ProposeCertifyAndAdmitAsync(
            CompositionDogfoodHarness.BuildProposerInput(ctx),
            witness,
            wiringMetadata: "composition-wiring-v0");

        handDecision.Admitted.Should().BeTrue();
        proposed.Decision.Admitted.Should().BeTrue();
        proposed.Decision.Record.CompositionEscapeRate.Should().Be(handDecision.Record.CompositionEscapeRate);
        proposed.Decision.Record.Signed.Should().Be(handDecision.Record.Signed);
        proposed.Decision.Record.CompositionId.Should().Be(handDecision.Record.CompositionId);
    }
}

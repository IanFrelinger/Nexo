using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Tests.Infrastructure.Certification.Dogfood;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for composition dogfood.</summary>
[Trait("Category", "Certification")]
public sealed class CompositionDogfoodTests
{
    [Fact]
    public async Task HonestComposition_StrongWitness_Admits_WithZeroEscapeRate()
    {
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();

        var decision = await ctx.CompositionAdmission.CertifyAndAdmitAsync(new CompositionCertificationRequest
        {
            Spec = CompositionDogfoodFixtures.HonestSpec(),
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeTrue(
            $"expected ADMIT, got {decision.FailureCheck}: {decision.Record.Reason}");
        decision.Record.CompositionEscapeRate.Should().Be(0);
        decision.Record.Signed.Should().BeTrue();
        ctx.CompositionSigner.Verify(decision.Record).Should().BeTrue();
    }

    [Fact]
    public async Task BrokenComposition_StrongWitness_Rejects()
    {
        var witness = CompositionDogfoodHarness.RequireHumanWitness();
        var ctx = await CompositionDogfoodHarness.CreateAdmittedContextAsync();

        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionDogfoodFixtures.BrokenWiringSpec(),
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse("broken composition wiring must not admit");
        decision.FailureCheck.Should().BeOneOf("seam", "correctness", "mutation");
    }
}

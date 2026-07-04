using FluentAssertions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Adaptation.Generation;
using Nexo.Infrastructure.Certification;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>Tests for generation safety.</summary>
[Trait("Category", "Adaptation")]
[Collection("GenerationSafety")]
public sealed class GenerationSafetyTests
{
    private const string BrickId = "line-substring-counter";
    private const string SampleText = "FOO line one\nplain\nFOO line two\n";
    private const string SampleSubstring = "FOO";

    private static readonly IntentSpec Intent = new(
        FixtureGeneratorModel.LineSubstringCounterIntentId,
        "Extract count of lines containing a given substring",
        BrickId: BrickId,
        Name: "Line Substring Counter");

    private static readonly WitnessSpec StrongWitness = new(
        BrickId,
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["text"] = SampleText,
                    ["substring"] = SampleSubstring
                },
                new Dictionary<string, object>
                {
                    ["matchCount"] = 2,
                    ["firstMatchingLine"] = "FOO line one"
                })
        ]);

    private static readonly WitnessSpec WeakWitness = new(
        BrickId,
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["text"] = SampleText,
                    ["substring"] = SampleSubstring
                },
                new Dictionary<string, object> { ["matchCount"] = 2 })
        ]);

    private static GenerateAndCertifyService CreateService(FixtureGeneratorModel fixture)
    {
        Environment.SetEnvironmentVariable("NEXO_CERT_NUGET_CONFIG", null);
        var store = new InMemoryCertificationRecordStore();
        var signer = new CertificationRecordSigner();
        var registry = new CertifiedBrickRegistry(store, signer);
        var gate = new CertificationGate(signer);
        var admission = new CertifiedBrickAdmission(gate, registry);
        var generator = new NewBrickGenerator(fixture);
        return new GenerateAndCertifyService(generator, admission);
    }

    [Fact]
    public async Task GoodGeneration_StrongWitness_Admits_WithZeroEscapeRate()
    {
        var fixture = new FixtureGeneratorModel { Variant = "correct" };
        var service = CreateService(fixture);
        var result = await service.GenerateCertifyAndAdmitAsync(Intent, StrongWitness);

        result.Decision.Admitted.Should().BeTrue(
            $"expected ADMIT, got {result.Decision.FailureCheck}: {result.Decision.Record.Reason}");
        result.Decision.Record.EscapeRate.Should().Be(0);
        result.Decision.Record.Signed.Should().BeTrue();
        result.Manifest!.GenerationProvenance.Should().StartWith("fixture:");
    }

    [Fact]
    public async Task BuggyGeneration_StrongWitness_Rejects()
    {
        var fixture = new FixtureGeneratorModel { Variant = "buggy" };
        var service = CreateService(fixture);
        var result = await service.GenerateCertifyAndAdmitAsync(Intent, StrongWitness);

        result.Decision.Admitted.Should().BeFalse("subtly wrong generation must not admit");
        result.Decision.FailureCheck.Should().BeOneOf("correctness", "mutation");
    }

    [Fact]
    public async Task GoodGeneration_WeakWitness_Rejects_WithTeeth()
    {
        var fixture = new FixtureGeneratorModel { Variant = "correct" };
        var service = CreateService(fixture);
        var result = await service.GenerateCertifyAndAdmitAsync(Intent, WeakWitness);

        result.Decision.Admitted.Should().BeFalse("weak witness must not admit when AST mutants survive");
        result.Decision.FailureCheck.Should().Be("mutation");
        result.Decision.Record.EscapeRate.Should().BeGreaterThan(0);
        result.Decision.Record.SurvivingMutantIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DependencyLeakGeneration_Rejects_DependencyCheck()
    {
        var fixture = new FixtureGeneratorModel { Variant = "dependency-leak" };
        var service = CreateService(fixture);
        var result = await service.GenerateCertifyAndAdmitAsync(Intent, StrongWitness);

        result.Decision.Admitted.Should().BeFalse();
        result.Decision.FailureCheck.Should().Be("dependency");
    }
}

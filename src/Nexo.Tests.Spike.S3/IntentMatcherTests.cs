using FluentAssertions;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Matching;
using Nexo.Spike.S3.Models;
using Xunit;

namespace Nexo.Tests.Spike.S3;

public sealed class IntentMatcherTests
{
    [Fact]
    public void Capability_key_enables_cross_context_reuse_matching()
    {
        var stored = new IntentDescriptor("csv-column-type-inference", ["core"], CapabilityKey: "csv-type-inference");
        var etlQuery = new IntentDescriptor("etl-pipeline-csv-inference", ["etl"], CapabilityKey: "csv-type-inference");

        IntentMatcher.Matches(etlQuery, stored).Should().BeTrue();
        IntentMatcher.ComputeLookupKey(etlQuery).Should().Be("csv-type-inference");
    }

    [Fact]
    public void Scripted_stand_in_generator_is_labeled_scripted_standin()
    {
        var generator = new ScriptedStandInSkillGenerator();
        ScriptedStandInSkillGenerator.BackendLabel.Should().Be("scripted-standin");

        var candidate = generator.Generate(
            new IntentDescriptor("csv-column-type-inference", ["core"], CapabilityKey: "csv-type-inference"));

        candidate.GeneratorBackend.Should().Be("scripted-standin");
        candidate.Hypothesis.Should().StartWith("StandIn:");
    }
}

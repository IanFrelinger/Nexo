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
    public async Task Recorded_generator_is_labeled_recorded_and_not_isolation_enforced()
    {
        var generator = new RecordedSkillGenerator();
        generator.BackendName.Should().Be("recorded");
        generator.IsolationEnforced.Should().BeFalse();

        var handoff = generator.Describe(
            new IntentDescriptor("csv-column-type-inference", ["core"], CapabilityKey: "csv-type-inference"));
        var candidate = await generator.IngestAsync(handoff);

        candidate.GeneratorBackend.Should().Be("recorded");
        candidate.Hypothesis.Should().StartWith("StandIn:");
    }
}

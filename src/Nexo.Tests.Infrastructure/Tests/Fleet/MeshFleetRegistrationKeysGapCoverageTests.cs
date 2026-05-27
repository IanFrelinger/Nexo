using FluentAssertions;
using Nexo.Infrastructure.Fleet;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Fleet;

public sealed class MeshFleetRegistrationKeysGapCoverageTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fingerprint_returns_null_for_blank_key(string? key)
    {
        MeshFleetRegistrationKeys.Fingerprint(key).Should().BeNull();
    }

    [Fact]
    public void Fingerprint_is_deterministic_and_truncated_to_sixteen_chars()
    {
        var first = MeshFleetRegistrationKeys.Fingerprint("peer-registration-key-123");
        var second = MeshFleetRegistrationKeys.Fingerprint("  peer-registration-key-123  ");

        first.Should().NotBeNull();
        first.Should().Be(second);
        first!.Length.Should().Be(16);
    }

    [Fact]
    public void IsDistinctFromDirectorKey_returns_true_when_director_key_missing()
    {
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("peer-key", null).Should().BeTrue();
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("peer-key", "   ").Should().BeTrue();
    }

    [Fact]
    public void IsDistinctFromDirectorKey_compares_trimmed_values()
    {
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey(" same ", "same").Should().BeFalse();
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("peer", "director").Should().BeTrue();
    }
}

public sealed class MeshElasticSchedulingOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_elastic_scheduling_configuration()
    {
        var options = new MeshElasticSchedulingOptions();

        MeshElasticSchedulingOptions.SectionPath.Should().Be("Nexo:Mesh:Elastic");
        options.Enabled.Should().BeFalse();
        options.IntervalMinutes.Should().Be(2);
        options.PendingStaleSeconds.Should().Be(120);
    }
}

public sealed class MeshPeerKnowledgeSyncOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_knowledge_sync_configuration()
    {
        var options = new MeshPeerKnowledgeSyncOptions();

        MeshPeerKnowledgeSyncOptions.SectionPath.Should().Be("Nexo:Mesh:KnowledgeSync");
        options.Enabled.Should().BeFalse();
        options.PeerBaseUrls.Should().BeEmpty();
        options.IntervalMinutes.Should().Be(15);
        options.SinceLookbackMultiplier.Should().Be(2);
        options.MaxAdaptations.Should().Be(500);
        options.MaxPatterns.Should().Be(500);
    }
}

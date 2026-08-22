using FluentAssertions;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for mesh peer knowledge sync options gap coverage.</summary>
public sealed class MeshPeerKnowledgeSyncOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_knowledge_sync_configuration()
    {
        var options = new MeshPeerKnowledgeSyncOptions();

        MeshPeerKnowledgeSyncOptions.SectionPath.Should().Be("Ashlar:Mesh:KnowledgeSync");
        options.Enabled.Should().BeFalse();
        options.PeerBaseUrls.Should().BeEmpty();
        options.IntervalMinutes.Should().Be(15);
        options.SinceLookbackMultiplier.Should().Be(2);
        options.MaxAdaptations.Should().Be(500);
        options.MaxPatterns.Should().Be(500);
    }
}

using FluentAssertions;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for mesh checkpoint options gap coverage.</summary>
public sealed class MeshCheckpointOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_mesh_checkpoint_configuration()
    {
        var options = new MeshCheckpointOptions();

        MeshCheckpointOptions.SectionPath.Should().Be("Ashlar:Mesh:Checkpoint");
        options.LeaseSeconds.Should().Be(1800);
        options.SweepEnabled.Should().BeFalse();
        options.SweepIntervalMinutes.Should().Be(1);
    }
}

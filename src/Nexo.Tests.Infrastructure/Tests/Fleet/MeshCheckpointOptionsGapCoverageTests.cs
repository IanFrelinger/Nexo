using FluentAssertions;
using Nexo.Infrastructure.Fleet;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Fleet;

public sealed class MeshCheckpointOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_mesh_checkpoint_configuration()
    {
        var options = new MeshCheckpointOptions();

        MeshCheckpointOptions.SectionPath.Should().Be("Nexo:Mesh:Checkpoint");
        options.LeaseSeconds.Should().Be(1800);
        options.SweepEnabled.Should().BeFalse();
        options.SweepIntervalMinutes.Should().Be(1);
    }
}

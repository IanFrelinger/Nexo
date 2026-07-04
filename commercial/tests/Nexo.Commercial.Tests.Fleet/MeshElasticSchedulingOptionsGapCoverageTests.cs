using FluentAssertions;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for mesh elastic scheduling options gap coverage.</summary>
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

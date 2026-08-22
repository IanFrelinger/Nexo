using FluentAssertions;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for lite db mesh director connection gap coverage.</summary>
[Collection(nameof(LiteDbFleetCollection))]
public sealed class LiteDbMeshDirectorConnectionGapCoverageTests
{
    [Fact]
    public void ToConnectionString_wraps_plain_path()
    {
        LiteDbMeshDirectorConnection.ToConnectionString("/tmp/mesh.db")
            .Should().Be("Filename=/tmp/mesh.db");
    }

    [Fact]
    public void ToConnectionString_preserves_existing_filename_prefix()
    {
        LiteDbMeshDirectorConnection.ToConnectionString("Filename=/tmp/custom.db")
            .Should().Be("Filename=/tmp/custom.db");
    }

    [Fact]
    public void ToConnectionString_rejects_blank_input()
    {
        var act = () => LiteDbMeshDirectorConnection.ToConnectionString("  ");

        act.Should().Throw<ArgumentNullException>().WithParameterName("pathOrConnectionString");
    }
}

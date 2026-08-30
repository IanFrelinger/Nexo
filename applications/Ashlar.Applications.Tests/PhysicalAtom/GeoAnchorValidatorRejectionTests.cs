using FluentAssertions;
using Ashlar.Certification.Physical;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Geo-anchor validator rejection tests: coordinate bounds, resolution bounds, and
/// H3 index shape checks each fail closed with a distinct reason.
/// </summary>
[Trait("Category", "Certification")]
public sealed class GeoAnchorValidatorRejectionTests
{
    private const string ConsistentH3 = "897016d01d3ffff";

    [Theory]
    [InlineData(-90.5)]
    [InlineData(91)]
    public void R1_LatitudeOutOfRange_Rejected(double latitude)
    {
        var anchor = new GeoAnchor(latitude, -122.4194, 9, ConsistentH3);

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeFalse();
        reason.Should().Contain("latitude");
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(180.5)]
    public void R2_LongitudeOutOfRange_Rejected(double longitude)
    {
        var anchor = new GeoAnchor(37.7749, longitude, 9, ConsistentH3);

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeFalse();
        reason.Should().Contain("longitude");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void R3_ResolutionOutOfRange_Rejected(int resolution)
    {
        var anchor = new GeoAnchor(37.7749, -122.4194, resolution, ConsistentH3);

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeFalse();
        reason.Should().Contain("resolution");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void R4_BlankH3Index_Rejected(string h3Index)
    {
        var anchor = new GeoAnchor(37.7749, -122.4194, 9, h3Index);

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeFalse();
        reason.Should().Contain("required");
    }

    [Theory]
    [InlineData("zzzzzzzzzzzzzzz")]
    [InlineData("897016d01d3fff")]
    [InlineData("897016d01d3ffff0")]
    public void R5_MalformedH3Index_Rejected(string h3Index)
    {
        var anchor = new GeoAnchor(37.7749, -122.4194, 9, h3Index);

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeFalse();
        reason.Should().Contain("15-character");
    }

    [Fact]
    public void R6_WellFormedButMismatchedH3Index_Rejected()
    {
        var anchor = new GeoAnchor(37.7749, -122.4194, 9, "000000000000000");

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeFalse();
        reason.Should().Contain("inconsistent");
    }

    [Fact]
    public void A1_ConsistentAnchor_AcceptedWithNullReason()
    {
        var anchor = new GeoAnchor(37.7749, -122.4194, 9, ConsistentH3);

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public void A2_UppercaseH3Index_AcceptedCaseInsensitively()
    {
        var anchor = new GeoAnchor(37.7749, -122.4194, 9, ConsistentH3.ToUpperInvariant());

        GeoAnchorValidator.IsConsistent(anchor, out var reason).Should().BeTrue();
        reason.Should().BeNull();
    }
}

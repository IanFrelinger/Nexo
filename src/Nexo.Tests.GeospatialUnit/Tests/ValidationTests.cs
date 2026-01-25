using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoTerrain.Validation;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Validation;
using Xunit;

namespace Nexo.Tests.GeospatialUnit.Tests;

public class ValidationTests
{
    private readonly ILogger _logger;

    public ValidationTests()
    {
        _logger = new LoggerFactory().CreateLogger("Test");
    }

    [Fact]
    public void DataIntegrityChecker_ComputeChecksum_ShouldReturnValidHash()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");

        // Act
        var checksum = DataIntegrityChecker.ComputeChecksum(data);

        // Assert
        checksum.Should().NotBeNullOrEmpty();
        checksum.Should().HaveLength(64); // SHA256 produces 64 hex characters
        checksum.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void DataIntegrityChecker_ComputeChecksum_ShouldThrow_WhenDataIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => DataIntegrityChecker.ComputeChecksum(null!));
    }

    [Fact]
    public void DataIntegrityChecker_ComputeChecksum_ShouldThrow_WhenDataIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => DataIntegrityChecker.ComputeChecksum(Array.Empty<byte>()));
    }

    [Fact]
    public void DataIntegrityChecker_ValidateChecksum_ShouldReturnTrue_WhenChecksumsMatch()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");
        var expectedChecksum = DataIntegrityChecker.ComputeChecksum(data);

        // Act
        var isValid = DataIntegrityChecker.ValidateChecksum(data, expectedChecksum, out var actualChecksum);

        // Assert
        isValid.Should().BeTrue();
        actualChecksum.Should().Be(expectedChecksum);
    }

    [Fact]
    public void DataIntegrityChecker_ValidateChecksum_ShouldReturnFalse_WhenChecksumsDontMatch()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");
        var wrongChecksum = "wrongchecksum";

        // Act
        var isValid = DataIntegrityChecker.ValidateChecksum(data, wrongChecksum, out var actualChecksum);

        // Assert
        isValid.Should().BeFalse();
        actualChecksum.Should().NotBe(wrongChecksum);
    }

    [Fact]
    public void DataIntegrityChecker_DetectCorruption_ShouldDetectNoDataValues()
    {
        // Arrange
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MinLongitude = new Longitude(0),
            MaxLatitude = new Latitude(1),
            MaxLongitude = new Longitude(1)
        };

        var heights = new float[100];
        Array.Fill(heights, float.NaN); // All NaN values

        var grid = new ElevationGrid(
            width: 10,
            height: 10,
            bounds: bounds,
            spacing: new GridSpacing(1, 1),
            heightsMeters: heights);

        // Act
        var report = DataIntegrityChecker.DetectCorruption(grid, _logger);

        // Assert
        report.Should().NotBeNull();
        report.NoDataCount.Should().Be(100);
    }

    [Fact]
    public void DataIntegrityChecker_DetectCorruption_ShouldDetectSuspiciousValues()
    {
        // Arrange
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(0),
            MinLongitude = new Longitude(0),
            MaxLatitude = new Latitude(1),
            MaxLongitude = new Longitude(1)
        };

        var heights = new float[100];
        Array.Fill(heights, 0f);
        heights[50] = 10000f; // Suspiciously high value

        var grid = new ElevationGrid(
            width: 10,
            height: 10,
            bounds: bounds,
            spacing: new GridSpacing(1, 1),
            heightsMeters: heights);

        // Act
        var report = DataIntegrityChecker.DetectCorruption(grid, _logger);

        // Assert
        report.Should().NotBeNull();
        report.SuspiciousValueCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DataIntegrityChecker_ValidateProjectionParameters_ShouldReturnTrue_ForValidBounds()
    {
        // Arrange
        var bounds = new GeoBounds
        {
            MinLatitude = new Latitude(37.0),
            MinLongitude = new Longitude(-122.0),
            MaxLatitude = new Latitude(37.1),
            MaxLongitude = new Longitude(-121.9)
        };

        // Act
        var isValid = DataIntegrityChecker.ValidateProjectionParameters(bounds, _logger);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void MeshQualityMetrics_ComputeTriangleQuality_ShouldCalculateMetrics()
    {
        // Arrange
        var mesh = new MeshData
        {
            Vertices = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0)
            },
            Indices = new List<int> { 0, 1, 2, 1, 3, 2 }
        };

        // Act
        var metrics = MeshQualityMetrics.ComputeTriangleQuality(mesh);

        // Assert
        metrics.Should().NotBeNull();
        metrics.AverageAspectRatio.Should().BeGreaterThan(0);
        metrics.MinAspectRatio.Should().BeGreaterThan(0);
        metrics.MaxAspectRatio.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MeshQualityMetrics_ComputeTriangleQuality_ShouldThrow_WhenMeshIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => MeshQualityMetrics.ComputeTriangleQuality(null!));
    }

    [Fact]
    public void MeshQualityMetrics_ComputeTriangleQuality_ShouldThrow_WhenVerticesAreNull()
    {
        // Arrange
        var mesh = new MeshData
        {
            Vertices = null!,
            Indices = new List<int> { 0, 1, 2 }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => MeshQualityMetrics.ComputeTriangleQuality(mesh));
    }

}

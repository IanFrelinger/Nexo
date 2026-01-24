using System.Security.Cryptography;
using System.Text;

namespace Nexo.GeoTerrain.Validation;

/// <summary>
/// Validates data integrity for elevation grids and downloaded tiles.
/// Provides checksum validation, corruption detection, and metadata consistency checks.
/// </summary>
public static class DataIntegrityValidator
{
    /// <summary>
    /// Computes a SHA-256 checksum for a byte array.
    /// </summary>
    public static string ComputeChecksum(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));

#if NET8_0_OR_GREATER
        var hash = SHA256.HashData(data);
#else
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
#endif
#if NETSTANDARD2_0
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
#else
        return Convert.ToHexString(hash).ToUpperInvariant();
#endif
    }

    /// <summary>
    /// Validates a checksum against expected value.
    /// </summary>
    public static bool ValidateChecksum(byte[] data, string expectedChecksum)
    {
        if (string.IsNullOrWhiteSpace(expectedChecksum))
            return true; // No expected checksum provided, skip validation

        var actual = ComputeChecksum(data);
        return string.Equals(actual, expectedChecksum.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detects potential corruption in an elevation grid by checking for:
    /// - Excessive NaN values (may indicate data loss)
    /// - Extreme values (outside reasonable Earth elevation ranges)
    /// - Sudden height changes (may indicate corruption)
    /// </summary>
    public static CorruptionReport DetectCorruption(ElevationGrid grid, float maxReasonableElevationMeters = 9000f, float maxSlopeChangeMeters = 1000f)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
#else
        if (grid == null)
            throw new ArgumentNullException(nameof(grid));
#endif

        var issues = new List<string>();
        var heights = grid.CloneHeightsMeters();
        var totalSamples = heights.Length;
        var nanCount = 0;
        var extremeCount = 0;
        var suspiciousCount = 0;

        // Check for excessive NaN values
        for (var i = 0; i < heights.Length; i++)
        {
            if (float.IsNaN(heights[i]))
            {
                nanCount++;
            }
            else
            {
                var h = heights[i];
                // Check for extreme values
                if (Math.Abs(h) > maxReasonableElevationMeters)
                {
                    extremeCount++;
                }
            }
        }

        var nanRatio = (double)nanCount / totalSamples;
        if (nanRatio > 0.5)
        {
            issues.Add($"Excessive no-data samples: {nanCount}/{totalSamples} ({nanRatio:P1})");
        }

        if (extremeCount > 0)
        {
            issues.Add($"Extreme elevation values detected: {extremeCount} samples outside ±{maxReasonableElevationMeters}m");
        }

        // Check for sudden height changes (potential corruption)
        for (var y = 1; y < grid.Height && suspiciousCount < 100; y++)
        {
            for (var x = 1; x < grid.Width && suspiciousCount < 100; x++)
            {
                var h1 = grid.GetHeightMeters(x - 1, y);
                var h2 = grid.GetHeightMeters(x, y);
                var h3 = grid.GetHeightMeters(x, y - 1);

                if (!float.IsNaN(h1) && !float.IsNaN(h2))
                {
                    var diffX = Math.Abs(h2 - h1);
                    if (diffX > maxSlopeChangeMeters)
                    {
                        suspiciousCount++;
                    }
                }

                if (!float.IsNaN(h1) && !float.IsNaN(h3))
                {
                    var diffY = Math.Abs(h1 - h3);
                    if (diffY > maxSlopeChangeMeters)
                    {
                        suspiciousCount++;
                    }
                }
            }
        }

        if (suspiciousCount >= 10)
        {
            issues.Add($"Suspicious height changes detected: {suspiciousCount} locations with >{maxSlopeChangeMeters}m change");
        }

        return new CorruptionReport
        {
            IsCorrupted = issues.Count > 0,
            Issues = issues,
            NanSampleCount = nanCount,
            ExtremeValueCount = extremeCount,
            SuspiciousChangeCount = suspiciousCount
        };
    }

    /// <summary>
    /// Validates coordinate system parameters for sanity.
    /// </summary>
    public static bool ValidateCoordinateSystem(GeoBounds bounds, GridSpacing spacing)
    {
        if (bounds == null)
            return false;

        try
        {
            // Check bounds are valid
            bounds.Validate();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        // Check spacing is reasonable (not zero, not negative, not extremely large)
        var avgSpacing = (spacing.MetersX + spacing.MetersY) / 2.0;
        if (avgSpacing <= 0 || avgSpacing > 100000)
            return false;

        // Check bounds width/height are reasonable given spacing
        var widthMeters = bounds.WidthMeters();
        var heightMeters = bounds.HeightMeters();

        if (widthMeters <= 0 || heightMeters <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Validates projection parameters with detailed error reporting.
    /// </summary>
    public static bool ValidateProjectionParameters(
        GeoBounds bounds,
        GridSpacing spacing,
        out string? error)
    {
        error = null;

        if (bounds == null)
        {
            error = "Bounds cannot be null";
            return false;
        }

        // Validate bounds
        if (bounds.MinLatitude.Degrees < -90 || bounds.MaxLatitude.Degrees > 90)
        {
            error = "Latitude bounds must be between -90 and 90 degrees";
            return false;
        }

        if (bounds.MinLongitude.Degrees < -180 || bounds.MaxLongitude.Degrees > 180)
        {
            error = "Longitude bounds must be between -180 and 180 degrees";
            return false;
        }

        if (bounds.MinLatitude.Degrees >= bounds.MaxLatitude.Degrees)
        {
            error = "Min latitude must be less than max latitude";
            return false;
        }

        if (bounds.MinLongitude.Degrees >= bounds.MaxLongitude.Degrees)
        {
            error = "Min longitude must be less than max longitude";
            return false;
        }

        // Validate spacing
        var latSpan = bounds.MaxLatitude.Degrees - bounds.MinLatitude.Degrees;
        var lonSpan = bounds.MaxLongitude.Degrees - bounds.MinLongitude.Degrees;

        // Convert spacing from meters to degrees (rough conversion)
        var spacingDegreesX = spacing.MetersX / 111320.0;
        var spacingDegreesY = spacing.MetersY / 111320.0;
        var avgSpacingDegrees = (spacingDegreesX + spacingDegreesY) / 2.0;

        // Spacing should be reasonable relative to bounds
        if (avgSpacingDegrees > latSpan || avgSpacingDegrees > lonSpan)
        {
            error = $"Grid spacing ({avgSpacingDegrees:F6}°) is larger than bounds span (lat: {latSpan}°, lon: {lonSpan}°)";
            return false;
        }

        // Spacing should not be too small (would create huge grids)
        if (avgSpacingDegrees < 0.000001) // ~0.1 meters
        {
            error = $"Grid spacing ({avgSpacingDegrees:F6}°) is unreasonably small";
            return false;
        }

        return true;
    }
}

/// <summary>
/// Report of corruption detection results.
/// </summary>
public sealed record CorruptionReport
{
    public required bool IsCorrupted { get; init; }
    public required IReadOnlyList<string> Issues { get; init; }
    public required int NanSampleCount { get; init; }
    public required int ExtremeValueCount { get; init; }
    public required int SuspiciousChangeCount { get; init; }
}

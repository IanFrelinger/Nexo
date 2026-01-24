using System.Security.Cryptography;
using System.Text;

namespace Nexo.GeoTerrain.Validation;

/// <summary>
/// Validates data integrity for elevation grids and downloaded tiles.
/// Provides checksum validation, corruption detection, and metadata consistency checks.
/// </summary>
public static class DataIntegrityChecker
{
    /// <summary>
    /// Computes a SHA256 checksum for a byte array.
    /// </summary>
    public static string ComputeChecksum(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Validates a checksum against expected value.
    /// </summary>
    public static bool ValidateChecksum(byte[] data, string expectedChecksum)
    {
        if (string.IsNullOrWhiteSpace(expectedChecksum))
            return true; // No expected checksum provided, skip validation

        var actual = ComputeChecksum(data);
        return string.Equals(actual, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detects potential corruption in an elevation grid by checking for:
    /// - Excessive NaN values (beyond expected no-data ratio)
    /// - Out-of-range values (beyond reasonable elevation bounds)
    /// - Sudden height changes (possible data corruption)
    /// </summary>
    public static CorruptionReport DetectCorruption(
        ElevationGrid grid,
        float maxReasonableElevationMeters = 9000f, // Mount Everest is ~8848m
        float minReasonableElevationMeters = -500f, // Dead Sea is ~-430m
        float maxSuddenChangeMeters = 1000f, // Max change between adjacent cells
        float maxNoDataRatio = 0.5f) // Max 50% no-data is reasonable
    {
        if (grid == null)
            throw new ArgumentNullException(nameof(grid));

        var issues = new List<string>();
        var heights = grid.CloneHeightsMeters();
        var nodataCount = 0;
        var outOfRangeCount = 0;
        var suddenChangeCount = 0;

        // Check each cell
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var h = heights[grid.Width * y + x];

                // Check for no-data
                if (float.IsNaN(h))
                {
                    nodataCount++;
                    continue;
                }

                // Check for out-of-range values
                if (h > maxReasonableElevationMeters || h < minReasonableElevationMeters)
                {
                    outOfRangeCount++;
                    if (outOfRangeCount == 1) // Only report once
                    {
                        issues.Add($"Out-of-range elevation detected: {h}m at ({x}, {y})");
                    }
                }

                // Check for sudden changes (compare with neighbors)
                if (x > 0)
                {
                    var left = heights[grid.Width * y + (x - 1)];
                    if (!float.IsNaN(left) && Math.Abs(h - left) > maxSuddenChangeMeters)
                    {
                        suddenChangeCount++;
                        if (suddenChangeCount == 1)
                        {
                            issues.Add($"Sudden elevation change detected: {Math.Abs(h - left)}m between ({x - 1}, {y}) and ({x}, {y})");
                        }
                    }
                }

                if (y > 0)
                {
                    var top = heights[grid.Width * (y - 1) + x];
                    if (!float.IsNaN(top) && Math.Abs(h - top) > maxSuddenChangeMeters)
                    {
                        suddenChangeCount++;
                        if (suddenChangeCount == 1)
                        {
                            issues.Add($"Sudden elevation change detected: {Math.Abs(h - top)}m between ({x}, {y - 1}) and ({x}, {y})");
                        }
                    }
                }
            }
        }

        // Check no-data ratio
        var noDataRatio = (float)nodataCount / (grid.Width * grid.Height);
        if (noDataRatio > maxNoDataRatio)
        {
            issues.Add($"Excessive no-data samples: {noDataRatio:P1} ({nodataCount}/{grid.Width * grid.Height})");
        }

        // Check for excessive issues
        var isCorrupted = issues.Count > 0 || 
                         outOfRangeCount > grid.Width * grid.Height * 0.01f || // More than 1% out of range
                         suddenChangeCount > grid.Width * grid.Height * 0.01f; // More than 1% sudden changes

        return new CorruptionReport
        {
            IsCorrupted = isCorrupted,
            Issues = issues,
            NoDataCount = nodataCount,
            OutOfRangeCount = outOfRangeCount,
            SuddenChangeCount = suddenChangeCount,
            NoDataRatio = noDataRatio
        };
    }

    /// <summary>
    /// Validates coordinate system parameters for sanity.
    /// </summary>
    public static bool ValidateProjectionParameters(
        GeoBounds bounds,
        GridSpacing spacing,
        out string? error)
    {
        error = null;

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

        // Spacing should be reasonable relative to bounds
        if (spacing.Degrees > latSpan || spacing.Degrees > lonSpan)
        {
            error = $"Grid spacing ({spacing.Degrees}°) is larger than bounds span (lat: {latSpan}°, lon: {lonSpan}°)";
            return false;
        }

        // Spacing should not be too small (would create huge grids)
        if (spacing.Degrees < 0.000001) // ~0.1 meters
        {
            error = $"Grid spacing ({spacing.Degrees}°) is unreasonably small";
            return false;
        }

        return true;
    }
}

/// <summary>
/// Report from corruption detection analysis.
/// </summary>
public sealed record CorruptionReport
{
    public required bool IsCorrupted { get; init; }
    public required IReadOnlyList<string> Issues { get; init; }
    public required int NoDataCount { get; init; }
    public required int OutOfRangeCount { get; init; }
    public required int SuddenChangeCount { get; init; }
    public required float NoDataRatio { get; init; }
}

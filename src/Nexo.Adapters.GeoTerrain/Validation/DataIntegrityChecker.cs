using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;

namespace Nexo.Adapters.GeoTerrain.Validation;

/// <summary>
/// Validates data integrity for elevation grids and downloaded tiles.
/// Provides checksum validation, corruption detection, and metadata consistency checks.
/// </summary>
public static class DataIntegrityChecker
{
    /// <summary>
    /// Computes SHA256 checksum of tile data.
    /// </summary>
    public static string ComputeChecksum(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Validates checksum of downloaded tile data.
    /// </summary>
    public static bool ValidateChecksum(byte[] data, string expectedChecksum, out string actualChecksum)
    {
        actualChecksum = ComputeChecksum(data);
        return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detects corruption in elevation grid data.
    /// </summary>
    public static CorruptionReport DetectCorruption(ElevationGrid grid, ILogger? logger = null)
    {
        if (grid == null)
            throw new ArgumentNullException(nameof(grid));

        var issues = new List<string>();
        var heights = grid.CloneHeightsMeters();
        var nodataCount = 0;
        var suspiciousCount = 0;
        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;

        // Check for suspicious values
        for (var i = 0; i < heights.Length; i++)
        {
            var v = heights[i];
            if (float.IsNaN(v))
            {
                nodataCount++;
                continue;
            }

            if (float.IsInfinity(v) || float.IsNegativeInfinity(v))
            {
                issues.Add($"Infinite value at index {i}");
                suspiciousCount++;
                continue;
            }

            // Check for suspiciously extreme values (outside reasonable Earth elevation range)
            if (v < -11000 || v > 9000) // Mariana Trench to Mount Everest + buffer
            {
                suspiciousCount++;
                if (suspiciousCount <= 10) // Only report first 10
                {
                    issues.Add($"Suspicious elevation value {v}m at index {i}");
                }
            }

            if (v < min) min = v;
            if (v > max) max = v;
        }

        // Check for all NaN (completely corrupted)
        if (nodataCount == heights.Length)
        {
            issues.Add("All elevation samples are NaN (completely corrupted grid)");
        }

        // Check for excessive NaN ratio (>50% suggests corruption)
        var nodataRatio = (double)nodataCount / heights.Length;
        if (nodataRatio > 0.5 && nodataCount > 100)
        {
            issues.Add($"Excessive no-data ratio: {nodataRatio:P2} ({nodataCount}/{heights.Length} samples)");
        }

        // Check bounds consistency
        if (grid.Bounds != null)
        {
            grid.Bounds.Validate();
        }

        var isCorrupted = issues.Count > 0;
        if (isCorrupted && logger != null)
        {
            logger.LogWarning("Elevation grid corruption detected: {Issues}", string.Join("; ", issues));
        }

        return new CorruptionReport
        {
            IsCorrupted = isCorrupted,
            Issues = issues,
            NoDataCount = nodataCount,
            SuspiciousValueCount = suspiciousCount,
            MinElevation = float.IsPositiveInfinity(min) ? null : min,
            MaxElevation = float.IsNegativeInfinity(max) ? null : max
        };
    }

    /// <summary>
    /// Validates coordinate system parameters for sanity.
    /// </summary>
    public static bool ValidateProjectionParameters(GeoBounds bounds, ILogger? logger = null)
    {
        if (bounds == null)
        {
            logger?.LogError("Bounds cannot be null for projection validation");
            return false;
        }

        try
        {
            bounds.Validate();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Bounds validation failed");
            return false;
        }

        // Check bounds are within valid Earth ranges
        if (bounds.MinLatitude.Degrees < -90 || bounds.MaxLatitude.Degrees > 90)
        {
            logger?.LogError("Latitude out of valid range [-90, 90]");
            return false;
        }

        if (bounds.MinLongitude.Degrees < -180 || bounds.MaxLongitude.Degrees > 180)
        {
            logger?.LogError("Longitude out of valid range [-180, 180]");
            return false;
        }

        if (bounds.MinLatitude.Degrees >= bounds.MaxLatitude.Degrees)
        {
            logger?.LogError("MinLatitude must be less than MaxLatitude");
            return false;
        }

        if (bounds.MinLongitude.Degrees >= bounds.MaxLongitude.Degrees)
        {
            logger?.LogError("MinLongitude must be less than MaxLongitude");
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
    public required int NoDataCount { get; init; }
    public required int SuspiciousValueCount { get; init; }
    public float? MinElevation { get; init; }
    public float? MaxElevation { get; init; }
}

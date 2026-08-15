using System.Text.RegularExpressions;
using H3;
using H3.Model;

namespace Nexo.Certification.Physical;

/// <summary>
/// Validates optional geo anchors: coordinate bounds and H3 index consistency with lat/lon.
/// </summary>
public static class GeoAnchorValidator
{
    private const int MinResolution = 0;
    private const int MaxResolution = 15;

    public static bool IsConsistent(GeoAnchor anchor, out string? reason)
    {
        if (anchor.Latitude is < -90 or > 90)
        {
            reason = "Geo anchor latitude must be within [-90, 90].";
            return false;
        }

        if (anchor.Longitude is < -180 or > 180)
        {
            reason = "Geo anchor longitude must be within [-180, 180].";
            return false;
        }

        if (anchor.Resolution is < MinResolution or > MaxResolution)
        {
            reason = $"Geo anchor resolution must be within [{MinResolution}, {MaxResolution}].";
            return false;
        }

        if (string.IsNullOrWhiteSpace(anchor.H3Index))
        {
            reason = "Geo anchor h3_index is required when geo_anchor is present.";
            return false;
        }

        if (!Regex.IsMatch(anchor.H3Index, "^[0-9a-fA-F]{15}$"))
        {
            reason = "Geo anchor h3_index must be a 15-character hexadecimal H3 cell index.";
            return false;
        }

        var expected = H3Index.FromLatLng(new LatLng(anchor.Latitude, anchor.Longitude), anchor.Resolution)
            .ToString()
            .ToLowerInvariant();
        var actual = anchor.H3Index.ToLowerInvariant();

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            reason = $"Geo anchor h3_index '{actual}' is inconsistent with lat/lon at resolution {anchor.Resolution} (expected '{expected}').";
            return false;
        }

        reason = null;
        return true;
    }
}

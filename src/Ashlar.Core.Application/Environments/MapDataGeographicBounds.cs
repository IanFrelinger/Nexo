namespace Ashlar.Core.Application.Environments;

/// <summary>
/// WGS84 geographic rectangle used when querying GIS-backed map data providers.
/// </summary>
/// <param name="MinLongitude">Western boundary in decimal degrees.</param>
/// <param name="MinLatitude">Southern boundary in decimal degrees.</param>
/// <param name="MaxLongitude">Eastern boundary in decimal degrees.</param>
/// <param name="MaxLatitude">Northern boundary in decimal degrees.</param>
public sealed record MapDataGeographicBounds(
    double MinLongitude,
    double MinLatitude,
    double MaxLongitude,
    double MaxLatitude);

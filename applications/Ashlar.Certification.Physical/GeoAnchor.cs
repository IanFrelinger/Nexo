namespace Ashlar.Certification.Physical;

/// <summary>
/// Optional geographic anchor binding a physical atom to a location on Earth.
/// </summary>
public sealed record GeoAnchor(
    double Latitude,
    double Longitude,
    int Resolution,
    string H3Index);

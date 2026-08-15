namespace Nexo.Certification.Physical.Resolution;

/// <summary>
/// Hosted digital-twin asset bytes keyed by content hash and semver version.
/// </summary>
public sealed record DigitalTwinAssetRecord(
    string AssetHash,
    string AssetVersion,
    string ContentType,
    byte[] AssetBytes);

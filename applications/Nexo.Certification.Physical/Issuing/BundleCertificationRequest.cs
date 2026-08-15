using Nexo.Certification.Physical;

namespace Nexo.Certification.Physical.Issuing;

/// <summary>
/// Input to deterministic physical-atom certificate issuance.
/// </summary>
public sealed class BundleCertificationRequest
{
    /// <summary>Physical atom identifier.</summary>
    public Guid AtomId { get; init; }

    /// <summary>Binding scope for the physical atom.</summary>
    public BindingScope BindingScope { get; init; }

    /// <summary>Raw asset bytes to certify.</summary>
    public required byte[] AssetBytes { get; init; }

    /// <summary>Asset version label.</summary>
    public required string AssetVersion { get; init; }

    /// <summary>Optional geographic anchor metadata.</summary>
    public GeoAnchor? GeoAnchor { get; init; }

    /// <summary>Optional manufacture metadata.</summary>
    public ManufactureMeta? ManufactureMeta { get; init; }

    /// <summary>Optional extension payloads keyed by name.</summary>
    public IReadOnlyDictionary<string, byte[]> Extensions { get; init; } =
        new Dictionary<string, byte[]>(StringComparer.Ordinal);
}

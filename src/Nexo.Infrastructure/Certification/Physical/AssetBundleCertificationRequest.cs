using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;

namespace Nexo.Infrastructure.Certification.Physical;

/// <summary>
/// Input to deterministic asset bundle certification and registration.
/// </summary>
public sealed class AssetBundleCertificationRequest
{
    /// <summary>Physical atom identifier.</summary>
    public Guid AtomId { get; init; }

    /// <summary>Binding scope for the physical atom.</summary>
    public BindingScope BindingScope { get; init; }

    /// <summary>Raw asset bytes to certify.</summary>
    public required byte[] AssetBytes { get; init; }

    /// <summary>Asset version label.</summary>
    public required string AssetVersion { get; init; }

    /// <summary>MIME content type of the asset bytes.</summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>Optional geographic anchor metadata.</summary>
    public GeoAnchor? GeoAnchor { get; init; }

    /// <summary>Optional manufacture metadata.</summary>
    public ManufactureMeta? ManufactureMeta { get; init; }

    /// <summary>Optional extension payloads keyed by name.</summary>
    public IReadOnlyDictionary<string, byte[]> Extensions { get; init; } =
        new Dictionary<string, byte[]>(StringComparer.Ordinal);
}

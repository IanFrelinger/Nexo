namespace Ashlar.Certification.Physical;

/// <summary>
/// Binds a physical atom to a hosted digital-twin asset via Ed25519 issuer attestation.
/// </summary>
public sealed record PhysicalAtomCertificate
{
    /// <summary>Current schema version for new certificates.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Schema version of this certificate payload.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Maturity level of the bound asset and certification process.</summary>
    public MaturityLevel Maturity { get; init; } = MaturityLevel.Prototype;

    /// <summary>Unique identifier of the physical atom.</summary>
    public Guid AtomId { get; init; }

    /// <summary>Scope governing how the atom may be bound to digital assets.</summary>
    public BindingScope BindingScope { get; init; }

    /// <summary>Lowercase hex SHA-256 digest (64 characters) of the bound asset bytes.</summary>
    public string AssetHash { get; init; } = string.Empty;

    /// <summary>SemVer of the bound asset.</summary>
    public string AssetVersion { get; init; } = string.Empty;

    /// <summary>Optional geographic anchor for spatially bound atoms.</summary>
    public GeoAnchor? GeoAnchor { get; init; }

    /// <summary>Optional manufacturing metadata for provenance tracking.</summary>
    public ManufactureMeta? ManufactureMeta { get; init; }

    /// <summary>Signed but uninterpreted by the core verifier.</summary>
    public IReadOnlyDictionary<string, byte[]> Extensions { get; init; } =
        new Dictionary<string, byte[]>(StringComparer.Ordinal);

    /// <summary>Base64-encoded Ed25519 signature over the canonical signing payload.</summary>
    public string? IssuerSignature { get; init; }
}

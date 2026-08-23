namespace Ashlar.Certification.Physical.Tagging;

/// <summary>
/// Compact physical-tag reference to a certified atom (QR/NFC payload content).
/// </summary>
public sealed record PhysicalAtomTagReference(
    TagReferenceKind Kind,
    Guid AtomId,
    byte[] AssetHash,
    string AssetVersion,
    byte[] IssuerFingerprint)
{
    public const int AssetHashLength = 32;
    public const int IssuerFingerprintLength = 8;
    public const int MaxAssetVersionLength = 64;

    public bool Equals(PhysicalAtomTagReference? other)
    {
        if (other is null)
            return false;

        return Kind == other.Kind
            && AtomId == other.AtomId
            && string.Equals(AssetVersion, other.AssetVersion, StringComparison.Ordinal)
            && AssetHash.AsSpan().SequenceEqual(other.AssetHash)
            && IssuerFingerprint.AsSpan().SequenceEqual(other.IssuerFingerprint);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        hash.Add(AtomId);
        hash.Add(AssetVersion, StringComparer.Ordinal);
        foreach (var b in AssetHash)
            hash.Add(b);
        foreach (var b in IssuerFingerprint)
            hash.Add(b);
        return hash.ToHashCode();
    }
}

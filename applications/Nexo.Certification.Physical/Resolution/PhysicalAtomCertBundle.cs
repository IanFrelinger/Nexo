namespace Nexo.Certification.Physical.Resolution;

/// <summary>
/// Self-contained certified bundle: certificate, asset bytes, and issuer public key for offline verification.
/// </summary>
public sealed record PhysicalAtomCertBundle(
    PhysicalAtomCertificate Certificate,
    byte[] AssetBytes,
    string ContentType,
    byte[] IssuerPublicKey);

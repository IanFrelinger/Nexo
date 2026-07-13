using System.Security.Cryptography;
using System.Text.Json;
using Nexo.Certification.Physical;
using Nexo.Provenance.Graph.Hashing;
using Nexo.Provenance.Graph.Models;
using NSec.Cryptography;

namespace Nexo.Provenance.Graph.Tests.Witness;

/// <summary>
/// Independent witness oracle — constructs and signs certs without the projector or issuance brick.
/// </summary>
public static class WitnessCertificateBuilder
{
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    public static (byte[] PrivateKey, byte[] PublicKey) CreateKeyPair()
    {
        using var key = Key.Create(
            Algorithm,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        return (
            key.Export(KeyBlobFormat.RawPrivateKey),
            key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
    }

    public static PhysicalAtomCertificate CreateUnsigned(
        string assetHash,
        Guid? atomId = null,
        BindingScope scope = BindingScope.Design) =>
        new()
        {
            AtomId = atomId ?? Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"),
            BindingScope = scope,
            AssetHash = assetHash,
            AssetVersion = "1.0.0"
        };

    public static PhysicalAtomCertificate Sign(PhysicalAtomCertificate unsigned, byte[] privateKey) =>
        unsigned with
        {
            IssuerSignature = PhysicalAtomCertificateSigning.Sign(unsigned, privateKey)
        };

    public static ProvenanceCertificateBundle CreateBundle(
        byte[] assetBytes,
        byte[] privateKey,
        byte[] publicKey,
        Action<BundleOptions>? configure = null)
    {
        var options = new BundleOptions();
        configure?.Invoke(options);

        var assetHash = AssetContentHasher.ComputeSha256Hex(assetBytes);
        var claims = new ProvenanceClaims
        {
            ArtifactKind = options.ArtifactKind,
            IssuedAt = options.IssuedAt,
            PolicyName = options.PolicyName,
            PolicyVersion = options.PolicyVersion,
            ProducerAgentId = options.ProducerAgentId,
            ProducerAgentKind = options.ProducerAgentKind,
            DependsOnArtifactIds = options.DependsOnArtifactIds,
            PriorCertificateHash = options.PriorCertificateHash
        };
        var unsigned = CreateUnsigned(assetHash, options.AtomId, options.BindingScope) with
        {
            Extensions = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [ProvenanceClaims.ExtensionKey] = ProvenanceClaimsCodec.Encode(claims)
            }
        };
        var signed = Sign(unsigned, privateKey);

        return new ProvenanceCertificateBundle
        {
            ArtifactId = assetHash,
            ArtifactKind = options.ArtifactKind,
            Certificate = signed,
            ArtifactContent = assetBytes,
            IssuerPublicKey = publicKey,
            IssuedAt = options.IssuedAt,
            PolicyName = options.PolicyName,
            PolicyVersion = options.PolicyVersion,
            ProducerAgentId = options.ProducerAgentId,
            ProducerAgentKind = options.ProducerAgentKind,
            DependsOnArtifactIds = options.DependsOnArtifactIds,
            PriorCertificateHash = options.PriorCertificateHash
        };
    }

    public static byte[] MutatePayloadByte(PhysicalAtomCertificate certificate)
    {
        var payload = PhysicalAtomCertificateSigning.BuildSigningPayload(certificate);
        payload[0] ^= 0xFF;
        return payload;
    }

    public sealed class BundleOptions
    {
        public ArtifactKind ArtifactKind { get; set; } = ArtifactKind.Atom;
        public Guid? AtomId { get; set; }
        public BindingScope BindingScope { get; set; } = BindingScope.Design;
        public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.Parse("2026-01-15T12:00:00Z");
        public string? PolicyName { get; set; }
        public string? PolicyVersion { get; set; }
        public string? ProducerAgentId { get; set; }
        public AgentKind? ProducerAgentKind { get; set; }
        public IReadOnlyList<string> DependsOnArtifactIds { get; set; } = Array.Empty<string>();
        public string? PriorCertificateHash { get; set; }
    }
}

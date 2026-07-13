using System.Text.Json;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Provenance.Graph.Models;

namespace Nexo.Provenance.Graph.Loading;

/// <summary>Loads <see cref="ProvenanceCertificateBundle"/> values from on-disk physical-atom cert artifacts.</summary>
public static class PhysicalAtomBundleLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static ProvenanceCertificateBundle LoadFromJsonFile(
        string path,
        ArtifactKind artifactKind = ArtifactKind.Atom,
        Action<ProvenanceCertificateBundle>? enrich = null)
    {
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<BundleDocument>(json, JsonOptions)
            ?? throw new InvalidDataException($"Unable to parse bundle document: {path}");

        if (doc.Certificate is null)
            throw new InvalidDataException($"Missing certificate in bundle: {path}");

        var assetBytes = doc.AssetBytesBase64 is not null
            ? Convert.FromBase64String(doc.AssetBytesBase64)
            : throw new InvalidDataException($"Missing assetBytesBase64 in bundle: {path}");

        var publicKey = doc.IssuerPublicKeyBase64 is not null
            ? Convert.FromBase64String(doc.IssuerPublicKeyBase64)
            : throw new InvalidDataException($"Missing issuerPublicKeyBase64 in bundle: {path}");

        var assetHash = AssetContentHasher.ComputeSha256Hex(assetBytes);
        var bundle = new ProvenanceCertificateBundle
        {
            ArtifactId = assetHash,
            ArtifactKind = artifactKind,
            Certificate = doc.Certificate,
            ArtifactContent = assetBytes,
            IssuerPublicKey = publicKey,
            IssuedAt = DateTimeOffset.UtcNow
        };

        enrich?.Invoke(bundle);
        return bundle;
    }

    public static PhysicalAtomCertBundle LoadPhysicalBundle(string path)
    {
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<BundleDocument>(json, JsonOptions)
            ?? throw new InvalidDataException($"Unable to parse bundle document: {path}");

        return new PhysicalAtomCertBundle(
            doc.Certificate!,
            Convert.FromBase64String(doc.AssetBytesBase64!),
            doc.ContentType ?? "application/octet-stream",
            Convert.FromBase64String(doc.IssuerPublicKeyBase64!));
    }

    public static IEnumerable<string> FindBundleFiles(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            yield break;

        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*.bundle.json", SearchOption.AllDirectories))
            yield return file;
    }

    private sealed class BundleDocument
    {
        public PhysicalAtomCertificate? Certificate { get; set; }
        public string? AssetBytesBase64 { get; set; }
        public string? IssuerPublicKeyBase64 { get; set; }
        public string? ContentType { get; set; }
    }
}

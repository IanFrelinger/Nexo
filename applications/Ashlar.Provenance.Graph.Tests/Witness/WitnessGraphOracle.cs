using System.Security.Cryptography;
using System.Text.Json;
using Ashlar.Certification.Physical;
using Ashlar.Provenance.Graph.Hashing;
using Ashlar.Provenance.Graph.Models;

namespace Ashlar.Provenance.Graph.Tests.Witness;

/// <summary>
/// Witness-independent edge oracle — derives expected graph edges from certificate bytes alone.
/// Does not reuse projector mapping logic.
/// </summary>
public static class WitnessGraphOracle
{
    public static IReadOnlyList<GraphEdge> DeriveEdges(ProvenanceCertificateBundle bundle)
    {
        var edges = new List<GraphEdge>();
        var certHash = ComputeCertHashWitness(bundle.Certificate);
        var claims = ParseSignedClaims(bundle.Certificate);

        edges.Add(new GraphEdge(bundle.Certificate.AssetHash, certHash, "CERTIFIED_BY"));

        if (claims.TryGetProperty("priorCertificateHash", out var prior))
            edges.Add(new GraphEdge(certHash, prior.GetString()!, "CHAINS_TO"));

        if (claims.TryGetProperty("policyName", out var policyName)
            && claims.TryGetProperty("policyVersion", out var policyVersion))
        {
            var policyId = $"{policyName.GetString()}@{policyVersion.GetString()}";
            edges.Add(new GraphEdge(certHash, policyId, "ISSUED_UNDER"));
        }

        if (claims.TryGetProperty("producerAgentId", out var producer))
            edges.Add(new GraphEdge(bundle.Certificate.AssetHash, producer.GetString()!, "PRODUCED_BY"));

        if (claims.TryGetProperty("dependsOnArtifactIds", out var dependencies))
        {
            foreach (var dependency in dependencies.EnumerateArray())
            {
                edges.Add(new GraphEdge(
                    bundle.Certificate.AssetHash,
                    dependency.GetString()!,
                    "DEPENDS_ON"));
            }
        }

        return edges;
    }

  /// <summary>
    /// Independent cert hash: SHA-256 of UTF-8 canonical JSON built from cert fields directly.
    /// </summary>
    private static string ComputeCertHashWitness(PhysicalAtomCertificate certificate)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes(BuildWitnessCanonicalJson(certificate));
        var signature = System.Text.Encoding.UTF8.GetBytes(certificate.IssuerSignature ?? string.Empty);
        var content = new byte[payload.Length + 1 + signature.Length];
        payload.CopyTo(content, 0);
        content[payload.Length] = (byte)'\n';
        signature.CopyTo(content, payload.Length + 1);
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static JsonElement ParseSignedClaims(PhysicalAtomCertificate certificate)
    {
        var bytes = certificate.Extensions[ProvenanceClaims.ExtensionKey];
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.Clone();
    }

    private static string BuildWitnessCanonicalJson(PhysicalAtomCertificate certificate)
    {
        var extensions = certificate.Extensions
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToDictionary(
                e => e.Key,
                e => Convert.ToBase64String(e.Value),
                StringComparer.Ordinal);

        var payload = new
        {
            schemaVersion = certificate.SchemaVersion,
            maturity = certificate.Maturity.ToString(),
            atomId = certificate.AtomId,
            bindingScope = certificate.BindingScope.ToString(),
            assetHash = certificate.AssetHash,
            assetVersion = certificate.AssetVersion,
            geoAnchor = certificate.GeoAnchor,
            manufactureMeta = certificate.ManufactureMeta,
            extensions
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });
    }

    public static bool EdgesMatchSnapshot(IReadOnlyList<GraphEdge> expected, IReadOnlyList<GraphEdge> actual)
    {
        var expectedSet = expected
            .Select(e => $"{e.FromId}|{e.Relationship}|{e.ToId}")
            .ToHashSet(StringComparer.Ordinal);

        var actualSet = actual
            .Select(e => $"{e.FromId}|{e.Relationship}|{e.ToId}")
            .ToHashSet(StringComparer.Ordinal);

        return expectedSet.SetEquals(actualSet);
    }

    public static string ComputeChainHeadWitness(IReadOnlyList<ProvenanceCertificateBundle> bundles)
    {
        var allHashes = bundles.Select(b => ComputeCertHashWitness(b.Certificate)).ToHashSet(StringComparer.Ordinal);
        var chainedFrom = bundles
            .Select(bundle => ParseSignedClaims(bundle.Certificate))
            .Where(claims => claims.TryGetProperty("priorCertificateHash", out _))
            .Select(claims => claims.GetProperty("priorCertificateHash").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var heads = allHashes.Where(h => !chainedFrom.Contains(h)).ToList();
        if (heads.Count != 1)
            throw new InvalidOperationException($"Witness found {heads.Count} chain heads.");
        return heads[0];
    }
}

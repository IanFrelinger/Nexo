using System.Security.Cryptography;
using System.Text.Json;
using Nexo.Certification.Physical;
using Nexo.Provenance.Graph.Hashing;
using Nexo.Provenance.Graph.Models;

namespace Nexo.Provenance.Graph.Tests.Witness;

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

        edges.Add(new GraphEdge(bundle.ArtifactId, certHash, "CERTIFIED_BY"));

        if (!string.IsNullOrWhiteSpace(bundle.PriorCertificateHash))
            edges.Add(new GraphEdge(certHash, bundle.PriorCertificateHash, "CHAINS_TO"));

        if (!string.IsNullOrWhiteSpace(bundle.PolicyName) && !string.IsNullOrWhiteSpace(bundle.PolicyVersion))
        {
            var policyId = $"{bundle.PolicyName}@{bundle.PolicyVersion}";
            edges.Add(new GraphEdge(certHash, policyId, "ISSUED_UNDER"));
        }

        if (!string.IsNullOrWhiteSpace(bundle.ProducerAgentId) && bundle.ProducerAgentKind.HasValue)
            edges.Add(new GraphEdge(bundle.ArtifactId, bundle.ProducerAgentId, "PRODUCED_BY"));

        foreach (var dep in bundle.DependsOnArtifactIds)
            edges.Add(new GraphEdge(bundle.ArtifactId, dep, "DEPENDS_ON"));

        return edges;
    }

  /// <summary>
    /// Independent cert hash: SHA-256 of UTF-8 canonical JSON built from cert fields directly.
    /// </summary>
    private static string ComputeCertHashWitness(PhysicalAtomCertificate certificate)
    {
        var witnessJson = BuildWitnessCanonicalJson(certificate);
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(witnessJson));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
        var allHashes = bundles.Select(b => ProvenanceCertificateHasher.ComputeCertificateHash(b.Certificate)).ToHashSet(StringComparer.Ordinal);
        var chainedFrom = bundles
            .Where(b => !string.IsNullOrWhiteSpace(b.PriorCertificateHash))
            .Select(b => b.PriorCertificateHash!)
            .ToHashSet(StringComparer.Ordinal);

        var heads = allHashes.Where(h => !chainedFrom.Contains(h)).ToList();
        return heads.Count == 1 ? heads[0] : heads.OrderBy(h => h, StringComparer.Ordinal).Last();
    }
}

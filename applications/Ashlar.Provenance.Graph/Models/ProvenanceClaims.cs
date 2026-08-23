using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ashlar.Provenance.Graph.Models;

/// <summary>
/// Graph claims carried inside the Ed25519-signed certificate extensions map.
/// Only claims decoded from this payload may create provenance relationships.
/// </summary>
public sealed record ProvenanceClaims
{
    public const string ExtensionKey = "nexo.provenance.v1";

    public ArtifactKind ArtifactKind { get; init; } = ArtifactKind.Atom;

    public DateTimeOffset? IssuedAt { get; init; }

    public string? PolicyName { get; init; }

    public string? PolicyVersion { get; init; }

    public string? ProducerAgentId { get; init; }

    public AgentKind? ProducerAgentKind { get; init; }

    public IReadOnlyList<string> DependsOnArtifactIds { get; init; } = Array.Empty<string>();

    public string? PriorCertificateHash { get; init; }
}

/// <summary>Canonical codec for signed provenance claims.</summary>
public static class ProvenanceClaimsCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static byte[] Encode(ProvenanceClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        Validate(claims);
        return JsonSerializer.SerializeToUtf8Bytes(claims, Options);
    }

    public static bool TryDecode(
        IReadOnlyDictionary<string, byte[]> extensions,
        out ProvenanceClaims? claims,
        out string? error)
    {
        claims = null;
        error = null;

        if (!extensions.TryGetValue(ProvenanceClaims.ExtensionKey, out var bytes))
            return true;

        try
        {
            claims = JsonSerializer.Deserialize<ProvenanceClaims>(bytes, Options)
                ?? throw new JsonException("Claims payload was null.");
            Validate(claims);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            error = ex.Message;
            claims = null;
            return false;
        }
    }

    private static void Validate(ProvenanceClaims claims)
    {
        if (string.IsNullOrWhiteSpace(claims.PolicyName) != string.IsNullOrWhiteSpace(claims.PolicyVersion))
            throw new ArgumentException("Policy name and version must be supplied together.");

        if (string.IsNullOrWhiteSpace(claims.ProducerAgentId) != !claims.ProducerAgentKind.HasValue)
            throw new ArgumentException("Producer id and kind must be supplied together.");

        if (claims.PriorCertificateHash is not null)
            ValidateHash(claims.PriorCertificateHash, nameof(claims.PriorCertificateHash));

        foreach (var dependency in claims.DependsOnArtifactIds)
            ValidateHash(dependency, nameof(claims.DependsOnArtifactIds));
    }

    private static void ValidateHash(string value, string field)
    {
        if (value.Length != 64
            || value.Any(c => !char.IsAsciiHexDigit(c))
            || !string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new ArgumentException($"{field} must be a 64-character lowercase SHA-256 hex digest.");
        }
    }
}

/// <summary>Verified, source-derived record accepted for atomic graph projection.</summary>
public sealed record VerifiedProvenanceRecord
{
    public required string ArtifactId { get; init; }

    public required ArtifactKind ArtifactKind { get; init; }

    public required string CertificateHash { get; init; }

    public DateTimeOffset? IssuedAt { get; init; }

    public required string SignerKeyId { get; init; }

    public string? PolicyName { get; init; }

    public string? PolicyVersion { get; init; }

    public string? ProducerAgentId { get; init; }

    public AgentKind? ProducerAgentKind { get; init; }

    public IReadOnlyList<string> DependsOnArtifactIds { get; init; } = Array.Empty<string>();

    public string? PriorCertificateHash { get; init; }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.Certification.Physical.Resolution;

/// <summary>
/// JSON manifest serialization for portable certified bundles (Phase 1, Prototype maturity).
/// </summary>
public static class PhysicalAtomCertBundleManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    public static string Serialize(PhysicalAtomCertBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var manifest = new ManifestDto(
            PhysicalAtomCertificate.CurrentSchemaVersion,
            MaturityLevel.Prototype.ToString(),
            bundle.Certificate,
            bundle.ContentType,
            Convert.ToBase64String(bundle.AssetBytes),
            Convert.ToBase64String(bundle.IssuerPublicKey));

        return JsonSerializer.Serialize(manifest, SerializerOptions);
    }

    public static PhysicalAtomCertBundle Deserialize(string manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
            throw new ArgumentException("Manifest JSON is required.", nameof(manifestJson));

        var dto = JsonSerializer.Deserialize<ManifestDto>(manifestJson, SerializerOptions)
            ?? throw new InvalidOperationException("Manifest JSON could not be parsed.");

        if (dto.SchemaVersion != PhysicalAtomCertificate.CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported manifest schema version {dto.SchemaVersion}.");

        return new PhysicalAtomCertBundle(
            dto.Certificate ?? throw new InvalidOperationException("Manifest certificate is required."),
            Convert.FromBase64String(dto.AssetBytesBase64 ?? throw new InvalidOperationException("Manifest assetBytesBase64 is required.")),
            dto.ContentType ?? "application/octet-stream",
            Convert.FromBase64String(dto.IssuerPublicKeyBase64 ?? throw new InvalidOperationException("Manifest issuerPublicKeyBase64 is required.")));
    }

    private sealed record ManifestDto(
        int SchemaVersion,
        string Maturity,
        PhysicalAtomCertificate? Certificate,
        string? ContentType,
        string? AssetBytesBase64,
        string? IssuerPublicKeyBase64);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

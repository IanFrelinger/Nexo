using System.Text.Json;
using FluentAssertions;
using Ashlar.Certification.Physical;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Replays the committed sample certificate against its documented asset bytes and issuer public key.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomSampleCertTests
{
    private static readonly string SampleRoot =
        Path.Combine(AppContext.BaseDirectory, "physical-atom-cert");

    [Fact]
    public void SampleInstanceScopeCert_VerifierAccepts()
    {
        var certJson = File.ReadAllText(Path.Combine(SampleRoot, "instance-scope.example.json"));
        var pubB64 = File.ReadAllText(Path.Combine(SampleRoot, "issuer-public-key.sample.b64")).Trim();

        using var doc = JsonDocument.Parse(certJson);
        var root = doc.RootElement;

        var cert = new PhysicalAtomCertificate
        {
            SchemaVersion = root.GetProperty("schemaVersion").GetInt32(),
            Maturity = Enum.Parse<MaturityLevel>(root.GetProperty("maturity").GetString()!),
            AtomId = root.GetProperty("atomId").GetGuid(),
            BindingScope = Enum.Parse<BindingScope>(root.GetProperty("bindingScope").GetString()!),
            AssetHash = root.GetProperty("assetHash").GetString()!,
            AssetVersion = root.GetProperty("assetVersion").GetString()!,
            GeoAnchor = ParseGeoAnchor(root.GetProperty("geoAnchor")),
            ManufactureMeta = ParseManufactureMeta(root.GetProperty("manufactureMeta")),
            Extensions = new Dictionary<string, byte[]>(StringComparer.Ordinal),
            IssuerSignature = root.GetProperty("issuerSignature").GetString()
        };

        var assetBytes = "sample-digital-twin-asset-v1"u8.ToArray();
        var issuerPublicKey = Convert.FromBase64String(pubB64);

        var result = PhysicalAtomCertificateVerifier.Verify(cert, assetBytes, issuerPublicKey);

        result.Trusted.Should().BeTrue();
    }

    private static GeoAnchor ParseGeoAnchor(JsonElement element) =>
        new(
            element.GetProperty("latitude").GetDouble(),
            element.GetProperty("longitude").GetDouble(),
            element.GetProperty("resolution").GetInt32(),
            element.GetProperty("h3Index").GetString()!);

    private static ManufactureMeta ParseManufactureMeta(JsonElement element) =>
        new(
            element.TryGetProperty("batchId", out var batch) ? batch.GetString() : null,
            element.TryGetProperty("serialNumber", out var serial) ? serial.GetString() : null,
            element.TryGetProperty("manufacturedAt", out var at)
                ? DateTimeOffset.Parse(at.GetString()!)
                : null);
}

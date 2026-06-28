using FluentAssertions;
using Nexo.Certification.Contracts;
using Nexo.Certification.State;
using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Infrastructure.Certification.Reuse;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CertifiedBehaviorCatalogTests
{
    private const string HmacKey = "attested-state-log-test-hmac";
    private const string Source = "certified-behavior-alpha-v1";

    [Fact]
    public void InMemoryCatalog_Miss_ReturnsNotFound()
    {
        var catalog = new InMemoryCertifiedBehaviorCatalog(Array.Empty<CertifiedBehaviorEntry>());
        var resolver = new CertifiedBehaviorCertificateResolver(catalog);

        var result = resolver.Resolve(BrickContentHasher.ComputeSha256("missing"));

        result.Found.Should().BeFalse();
    }

    [Fact]
    public void FileCatalog_ResolvesTrustedBehavior_ForStateLogVerifier()
    {
        var entry = CreateEntry();
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-behavior-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            FileCertifiedBehaviorCatalog.WriteEntry(tempDir, entry);
            var catalog = new FileCertifiedBehaviorCatalog(tempDir);
            var resolver = new CertifiedBehaviorCertificateResolver(catalog);

            var resolve = resolver.Resolve(entry.ContentHash);
            resolve.Found.Should().BeTrue();

            var trust = CertificationTrustVerifier.Verify(resolve.Record!, resolve.BrickSource!, HmacKey);
            trust.Trusted.Should().BeTrue($"expected TRUSTED, got {trust.FailureCode}: {trust.Reason}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static CertifiedBehaviorEntry CreateEntry()
    {
        var record = new CertificationRecordData
        {
            Status = "PASS",
            Stage = "witness",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            BrickId = "behavior-alpha",
            ContentHash = BrickContentHasher.ComputeSha256(Source),
            EscapeRate = 0,
            TotalMutants = 1,
            SurvivingMutants = 0,
            KilledMutants = new[] { "m1" },
            SurvivingMutantIds = Array.Empty<string>()
        };

        record = record with { Signature = CertificationRecordSigning.Sign(record, HmacKey) };

        return new CertifiedBehaviorEntry
        {
            ContentHash = record.ContentHash!,
            Record = record,
            Source = Source
        };
    }
}

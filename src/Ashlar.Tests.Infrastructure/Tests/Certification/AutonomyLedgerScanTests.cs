using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.HotSwap;
using NSec.Cryptography;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The ledger scan behind the digest (autonomy spec R5.4 + §8 record-store leg): the
/// store now enumerates VERIFIABLE records only, the scan flags revocation-chain suspects
/// and depth contradictions over them, and the digest renders the findings — or an
/// explicit "clean" line, because a scan that ran and found nothing must be
/// distinguishable from a scan that never ran.
/// </summary>
[Trait("Category", "Certification")]
public sealed class AutonomyLedgerScanTests
{
    [Fact]
    public void FileStore_All_ExcludesTamperedRecords_ExactlyLikePointLookups()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ashlar-ledger-{Guid.NewGuid():N}");
        var (privateKey, _) = CreateEd25519Key();
        var signer = new CertificationRecordSigner(ed25519PrivateKeyBase64: privateKey);
        var store = new FileCertificationRecordStore(directory, signer);

        store.Save(signer.SignRecord(Record("honest-brick", "sha256:honest")));

        // An attacker edits a record on disk: flips Admitted on a copy with no valid signature.
        File.WriteAllText(
            Path.Combine(directory, "tampered-brick.json"),
            """{ "status": "PASS", "stage": "S0-S2", "admitted": true, "signed": true, "timestamp": "2026-01-01T00:00:00Z", "brickId": "tampered-brick", "contentHash": "sha256:evil", "signature": "forged" }""");

        var all = store.All();

        all.Should().ContainSingle().Which.BrickId.Should().Be("honest-brick",
            "a record that fails verification is absent from the ledger scan, never surfaced as data");
    }

    [Fact]
    public void Scan_FlagsChainSuspectsAndDepthContradictions_AndTheDigestRendersThem()
    {
        var store = new InMemoryCertificationRecordStore();
        var revocations = new InMemoryCertificateRevocationList();
        revocations.Revoke("sha256:quarantined", "watch breach");

        // Clean record.
        store.Save(Record("clean-brick", "sha256:clean"));
        // Built on the quarantined artifact: suspect (R5.4).
        store.Save(Record("suspect-brick", "sha256:suspect") with
        {
            Inputs = new[]
            {
                new Ashlar.Certification.Contracts.CertificationInput
                {
                    Kind = "artifact", Id = "parent", Hash = "sha256:quarantined"
                }
            }
        });
        // Laundered pair: same content hash certified at depth 2 and (implicitly) depth 0.
        store.Save(Record("laundered-v1", "sha256:laundered") with
        {
            Inputs = new[]
            {
                new Ashlar.Certification.Contracts.CertificationInput
                {
                    Kind = "generation-depth", Id = "2", Hash = "sha256:chain"
                }
            }
        });
        store.Save(Record("laundered-v2", "sha256:laundered"));

        var report = AutonomyLedgerScan.Run(store, revocations);

        report.IsClean.Should().BeFalse();
        report.RevocationSuspects.Should().ContainSingle().Which.BrickId.Should().Be("suspect-brick");
        report.LaunderingContradictions.Should().ContainSingle().Which.ContentHash.Should().Be("sha256:laundered");

        var digest = AutonomyDigest.Render(
            Array.Empty<BrickSwapProvenanceEvent>(), held: null, ledgerScan: report);
        digest.Should().Contain("## Ledger scan")
            .And.Contain("**SUSPECT** suspect-brick")
            .And.Contain("re-verification (R5.4)")
            .And.Contain("**DEPTH CONTRADICTION** sha256:laundered")
            .And.Contain("[0, 2]");
    }

    [Fact]
    public void CleanLedger_RendersAsExplicitlyClean()
    {
        var store = new InMemoryCertificationRecordStore();
        store.Save(Record("clean-brick", "sha256:clean"));

        var report = AutonomyLedgerScan.Run(store, new InMemoryCertificateRevocationList());

        report.IsClean.Should().BeTrue();
        AutonomyDigest.Render(Array.Empty<BrickSwapProvenanceEvent>(), null, report)
            .Should().Contain("clean: no revocation-chain suspects, no depth contradictions");
    }

    private static CertificationRecord Record(string brickId, string contentHash) => new()
    {
        SchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
        Status = "PASS",
        Stage = "S0-S2",
        Admitted = true,
        Signed = true,
        Timestamp = DateTimeOffset.UnixEpoch,
        BrickId = brickId,
        ContentHash = contentHash,
        Gate = "ledger-scan-test-harness",
    };

    private static (string PrivateKeyBase64, string PublicKeyBase64) CreateEd25519Key()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        return (
            Convert.ToBase64String(key.Export(KeyBlobFormat.RawPrivateKey)),
            Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)));
    }
}

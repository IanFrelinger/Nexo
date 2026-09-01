using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// A save must never be able to destroy the verdict already on disk.
///
/// <para>The record file is the admission evidence: <c>Get</c> returns null for anything it
/// cannot parse and every caller reads "no record" as uncertified, so shredding a record does
/// not merely lose information — it silently un-admits an admitted brick. Writing in place
/// truncates first, so any failure partway leaves exactly that wreckage; the store therefore
/// stages to a sibling file and moves it into place.</para>
///
/// <para>The regression pinned here is the staging file's NAME. This store is a DI singleton and
/// two callers may certify the same brick at once (the brick registry and the composition
/// registry both hold it). With a fixed <c>{brick}.json.tmp</c>, both writers land on one file:
/// the second truncates the first's staged bytes mid-flight and the first then moves that partial
/// file over the good record, reintroducing the very shredding the staging exists to prevent —
/// and a failing writer's cleanup deletes a healthy writer's staged file. Per-call staging names
/// are what make the mechanism actually hold under concurrency.</para>
/// </summary>
[Trait("Category", "Certification")]
[Trait("Category", "ProdStyle")]
public sealed class FileCertificationRecordStoreConcurrencyTests : TempDirTestBase
{
    private const string BrickId = "concurrent.brick";

    public FileCertificationRecordStoreConcurrencyTests()
        : base("ashlar-cert-store-concurrency")
    {
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task ConcurrentSavesOfOneBrick_LeaveAVerifiableRecord_NeverAShreddedOne()
    {
        var signer = new CertificationRecordSigner();
        var store = new FileCertificationRecordStore(TempDir, signer);

        // Every writer saves an ADMITTED record, so a shredded file is unmistakable: the brick
        // reads back as uncertified even though nothing ever refused it.
        Parallel.For(0, 64, i => store.Save(signer.SignRecord(Admitted(i))));

        store.Get(BrickId).Should().NotBeNull(
            "one of the concurrent writers' records must survive intact — an unparseable file " +
            "reads as absent, which silently un-admits an admitted brick");
        store.IsAdmitted(BrickId).Should().BeTrue("every writer wrote an admission");
        store.All().Should().ContainSingle(r => r.BrickId == BrickId);

        Directory.EnumerateFiles(TempDir)
            .Select(Path.GetFileName)
            .Should().ContainSingle()
            .Which.Should().Be(BrickId + ".json", "no staging file may outlive a successful save");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task AStrayStagingFile_IsNotReadAsARecord()
    {
        // A crash between staging and moving leaves a staging file behind. It must be inert:
        // All() scans the directory as ledger evidence, and unverified JSON is not evidence.
        var signer = new CertificationRecordSigner();
        var store = new FileCertificationRecordStore(TempDir, signer);
        store.Save(signer.SignRecord(Admitted(0)));

        File.WriteAllText(
            Path.Combine(TempDir, $"{BrickId}.json.{Guid.NewGuid():N}.tmp"),
            "{\"brickId\":\"forged.brick\",\"status\":\"PASS\",\"admitted\":true,\"signed\":true}");

        store.All().Should().ContainSingle().Which.BrickId.Should().Be(BrickId);
        store.IsAdmitted("forged.brick").Should().BeFalse();

        return Task.CompletedTask;
    }

    private static CertificationRecord Admitted(int seed) => new()
    {
        Status = "PASS",
        Stage = "admitted",
        Admitted = true,
        Signed = true,
        Timestamp = DateTimeOffset.UtcNow,
        BrickId = BrickId,
        ContentHash = $"hash-{seed}",
        Reason = $"writer {seed}",
    };
}

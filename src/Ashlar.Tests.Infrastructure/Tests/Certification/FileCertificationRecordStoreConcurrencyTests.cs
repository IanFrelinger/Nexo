using System.Collections.Concurrent;
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

        // Dedicated threads behind a barrier, deliberately NOT Parallel.For. Two reasons, and the
        // second one cost a CI run to learn:
        //
        // 1. The race needs the writers to be INSIDE Save at the same instant. A barrier guarantees
        //    that; Parallel.For only promises the work happens, and may well run it sequentially.
        // 2. Parallel.For dispatches onto the thread pool, and this assembly runs test collections
        //    concurrently (xunit.runner.json: maxParallelThreads 2). Flooding the pool with dozens
        //    of blocking file writes starves the yield-based retry loops other tests depend on —
        //    CertifiedBrickHotSwapHostTests asserts an AssemblyLoadContext was collected within a
        //    bounded number of GC passes, and it fails when this test hogs the pool beside it.
        //    Owning our threads keeps the blast radius of a concurrency test inside the test.
        //
        // Eight writers is plenty: the defect needs two racers, not sixty-four.
        const int writers = 8;
        const int savesPerWriter = 4;
        var startTogether = new Barrier(writers);
        var failures = new ConcurrentBag<Exception>();

        var threads = Enumerable.Range(0, writers)
            .Select(w => new Thread(() =>
            {
                try
                {
                    startTogether.SignalAndWait();
                    for (var i = 0; i < savesPerWriter; i++)
                        store.Save(signer.SignRecord(Admitted((w * savesPerWriter) + i)));
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            })
            { IsBackground = true, Name = $"cert-store-writer-{w}" })
            .ToList();

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // With a shared staging name this is where the old code died: one writer's cleanup deletes
        // the file another writer staged, so the healthy writer's File.Move throws FileNotFound.
        failures.Should().BeEmpty("a save must not fail merely because another save was in flight");

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
    public Task ReplaceRetriesWindowsSharingDenial_ThenLandsTheStagedRecord()
    {
        var dest = Path.Combine(TempDir, "retry-dest.json");
        var staged = dest + $".{Guid.NewGuid():N}.tmp";
        File.WriteAllText(dest, "previous-verdict");
        File.WriteAllText(staged, "new-verdict");

        var attempts = 0;
        AtomicRecordReplace.IntoPlace(staged, dest, (source, destination) =>
        {
            attempts++;
            if (attempts < 3)
                throw new UnauthorizedAccessException("Access to the path is denied.");
            File.Move(source, destination, overwrite: true);
        });

        attempts.Should().Be(3, "two sharing denials must be retried, not surfaced");
        File.ReadAllText(dest).Should().Be("new-verdict");
        File.Exists(staged).Should().BeFalse();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task ReplaceExhaustsRetries_ThenThrows_LeavingThePreviousVerdict()
    {
        var dest = Path.Combine(TempDir, "retry-exhausted.json");
        var staged = dest + $".{Guid.NewGuid():N}.tmp";
        File.WriteAllText(dest, "previous-verdict");
        File.WriteAllText(staged, "new-verdict");

        var attempts = 0;
        var act = () => AtomicRecordReplace.IntoPlace(staged, dest, (_, _) =>
        {
            attempts++;
            throw new UnauthorizedAccessException("Access to the path is denied.");
        });

        act.Should().Throw<UnauthorizedAccessException>();
        attempts.Should().Be(AtomicRecordReplace.MaxAttempts);
        File.ReadAllText(dest).Should().Be("previous-verdict", "a persistent denial must not shred the live record");
        File.Exists(staged).Should().BeTrue("the staged file stays for the caller to clean up");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task LoadRefusal_PersistsAVerifiableFail_AndNeverAdmits()
    {
        var signer = new CertificationRecordSigner();
        var store = new FileCertificationRecordStore(TempDir, signer);
        var record = LoadRefusalRecord.Create(
            signer,
            BrickId,
            "il-import fence: P/Invoke to libc!exit");

        store.Save(record);

        var loaded = store.Get(BrickId);
        loaded.Should().NotBeNull("a load refuse must be evidence, not an absent file");
        loaded!.Admitted.Should().BeFalse();
        loaded.Status.Should().Be("FAIL");
        loaded.Stage.Should().Be(LoadRefusalRecord.Stage);
        loaded.Reason.Should().Contain("P/Invoke");
        store.IsAdmitted(BrickId).Should().BeFalse();
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

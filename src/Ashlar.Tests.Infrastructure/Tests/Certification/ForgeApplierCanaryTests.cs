using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.Core.Application.Certification.Ports;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// A4 (the safety envelope): the apply path must be TRANSACTIONAL, and an auto-admitted change that
/// fails the post-apply canary must be ROLLED BACK — never left on an unattended node. These pin:
/// (1) a mid-batch write failure restores the whole batch; (2) a canary rejection reverts every write
/// (deleting a new file, restoring a modified one's prior bytes) and rejects the proposals;
/// (3) a passing canary commits; (4) fail-closed — a verifier error rolls back too.
///
/// <para>In <c>...Tests.Certification</c> so it rides cert-gate. Hermetic: a temp dir, no network.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class ForgeApplierCanaryTests : IDisposable
{
    private readonly string _repoRoot;
    private readonly ChangeProposalStore _store;

    public ForgeApplierCanaryTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "ashlar-forge-canary-" + Guid.NewGuid().ToString("N")[..12]);
        _repoRoot = Path.Combine(baseDir, "repo");
        Directory.CreateDirectory(_repoRoot);
        _store = new ChangeProposalStore(Path.Combine(baseDir, "forge"));
    }

    public void Dispose()
    {
        try { Directory.Delete(Path.GetDirectoryName(_repoRoot)!, recursive: true); } catch { /* best effort */ }
    }

    // ---- The post-apply canary ----------------------------------------------------------------

    [Fact]
    public async Task Verified_canaryPasses_appliesAndCommits()
    {
        var id = Add("src/Feature.cs", "namespace Demo; public sealed class Feature { }");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id }, _repoRoot, "test", Stub.Passing());

        outcome.DidApply.Should().BeTrue(outcome.Reason);
        outcome.AppliedPaths.Should().ContainSingle().Which.Should().Be("src/Feature.cs");
        File.Exists(Path.Combine(_repoRoot, "src", "Feature.cs")).Should().BeTrue();
        _store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    [Fact]
    public async Task Verified_canaryFails_revertsANewFile_andRejects()
    {
        var target = Path.Combine(_repoRoot, "src", "New.cs");
        var id = Add("src/New.cs", "namespace Demo; public sealed class New { }");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id }, _repoRoot, "test", Stub.Failing("nope"));

        outcome.DidApply.Should().BeFalse();
        outcome.RolledBack.Should().BeTrue();
        outcome.Reason.Should().Contain("nope");
        outcome.UnrestoredPaths.Should().BeEmpty();
        File.Exists(target).Should().BeFalse("a new file the canary rejected must be deleted, not left on the node");
        _store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Rejected);
    }

    [Fact]
    public async Task Verified_canaryFails_restoresAModifiedFilesPriorBytes()
    {
        var full = Path.Combine(_repoRoot, "src", "Existing.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "ORIGINAL");
        var id = Add("src/Existing.cs", "REPLACEMENT");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id }, _repoRoot, "test", Stub.Failing("bad"));

        outcome.RolledBack.Should().BeTrue();
        File.ReadAllText(full).Should().Be("ORIGINAL", "a modified file must be restored to its exact prior content");
        _store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Rejected);
    }

    [Fact]
    public async Task Verified_verifierThrows_isFailClosed_andRollsBack()
    {
        var full = Path.Combine(_repoRoot, "src", "X.cs");
        var id = Add("src/X.cs", "namespace Demo; public sealed class X { }");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id }, _repoRoot, "test", Stub.Throwing());

        outcome.DidApply.Should().BeFalse("a verifier error must fail closed, never admit");
        outcome.Reason.Should().Contain("canary errored");
        File.Exists(full).Should().BeFalse();
        _store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Rejected);
    }

    [Fact]
    public async Task Verified_realRoslynCanary_rejectsCodeThatDoesNotCompile()
    {
        var full = Path.Combine(_repoRoot, "src", "Broken.cs");
        var id = Add("src/Broken.cs", "namespace Demo; public sealed class Broken { public int Oops() => ; }");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id }, _repoRoot, "test", new Ashlar.Infrastructure.Certification.RoslynPostApplyVerification());

        outcome.RolledBack.Should().BeTrue("code that does not compile must not survive on the node");
        outcome.Reason.Should().Contain("post-apply error");
        File.Exists(full).Should().BeFalse();
    }

    [Fact]
    public async Task Verified_realRoslynCanary_admitsCodeThatCompiles()
    {
        var full = Path.Combine(_repoRoot, "src", "Ok.cs");
        var id = Add("src/Ok.cs", "namespace Demo; public sealed class Ok { public int N() => 1; }");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id }, _repoRoot, "test", new Ashlar.Infrastructure.Certification.RoslynPostApplyVerification());

        outcome.DidApply.Should().BeTrue(outcome.Reason);
        File.ReadAllText(full).Should().Contain("class Ok");
        _store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    [Fact]
    public async Task Verified_verifierReturnsNull_isFailClosed_andRollsBack()
    {
        var full = Path.Combine(_repoRoot, "src", "N.cs");
        var id = Add("src/N.cs", "namespace Demo; public sealed class N { }");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id }, _repoRoot, "test", Stub.Null());

        outcome.DidApply.Should().BeFalse("a null verdict is 'no decision' and must not admit");
        outcome.Reason.Should().Contain("no result");
        File.Exists(full).Should().BeFalse();
        _store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Rejected);
    }

    [Fact]
    public async Task Verified_duplicateIdInBatch_appliesOnce_noThrow()
    {
        var id = Add("src/Once.cs", "namespace Demo; public sealed class Once { }");

        var outcome = await ForgeApplier.ApplyAllWithVerificationAsync(
            _store, new[] { id, id }, _repoRoot, "test", Stub.Passing());

        outcome.DidApply.Should().BeTrue("a repeated id must not throw on the second pass");
        outcome.AppliedPaths.Should().ContainSingle();
        _store.Find(id)!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    // ---- Transactional apply (no canary) -------------------------------------------------------

    [Fact]
    public void ApplyAll_writeFailsMidBatch_rollsBackTheAlreadyWrittenFile()
    {
        // First target is an ordinary new file; the second's path is occupied by a DIRECTORY, so its
        // File.WriteAllText throws after the first has already been written. The batch must roll back.
        var good = Add("src/good.cs", "// good");
        var blockedPath = Path.Combine(_repoRoot, "src", "blocked.cs");
        Directory.CreateDirectory(blockedPath);          // a dir where a file write is expected
        var bad = Add("src/blocked.cs", "// blocked");

        var act = () => ForgeApplier.ApplyAll(_store, new[] { good, bad }, _repoRoot, "test");

        act.Should().Throw<InvalidOperationException>().WithMessage("*rolled back*");
        File.Exists(Path.Combine(_repoRoot, "src", "good.cs"))
            .Should().BeFalse("a mid-batch write failure must roll back files already written in the batch");
        _store.Find(good)!.Status.Should().Be(ChangeProposalStatus.Proposed, "a rolled-back batch marks nothing applied");
    }

    private string Add(string target, string content)
    {
        var id = "p-" + Guid.NewGuid().ToString("N")[..16];
        _store.Add(new ChangeProposal
        {
            Id = id,
            TargetPath = target,
            NewContent = content,
            Summary = "test",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        return id;
    }

    /// <summary>A controllable canary for the rollback/commit paths.</summary>
    private sealed class Stub : IPostApplyVerification
    {
        private readonly Func<PostApplyVerificationResult> _fn;
        private Stub(Func<PostApplyVerificationResult> fn) => _fn = fn;

        public static Stub Passing() => new(() => new PostApplyVerificationResult(true, "ok"));
        public static Stub Failing(string why) => new(() => new PostApplyVerificationResult(false, why));
        public static Stub Throwing() => new(() => throw new InvalidOperationException("boom"));
        public static Stub Null() => new(() => null!);

        public Task<PostApplyVerificationResult> VerifyAsync(
            string repoRoot, IReadOnlyList<AppliedFile> applied, CancellationToken cancellationToken = default)
            => Task.FromResult(_fn());
    }
}

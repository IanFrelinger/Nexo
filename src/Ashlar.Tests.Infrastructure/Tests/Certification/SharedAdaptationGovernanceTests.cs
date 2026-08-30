using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Infrastructure.Adaptation;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The mesh shared-adaptation adopt path is a mediated writer of UNTRUSTED peer content, and before
/// Phase 2's follow-up it wrote <c>entry.Files</c> straight into <c>Path.Combine(repoRoot, path)</c>
/// with no containment (so <c>../../x</c> escaped the repo), no governance floor, and no reparse
/// check — then ran <c>dotnet build</c> on the result. This pins it shut: adoption now routes every
/// file through the same <see cref="Ashlar.Core.Application.Paths.MediatedWritePath"/> floor as the
/// forge apply path, rejecting the whole entry before any write.
///
/// <para>Isolated by an injected <c>repoRoot</c> (a temp dir with a fake <c>Ashlar.sln</c>) so it
/// never touches the real tree, and a stub regression runner that "passes" — proving a benign file
/// really is adopted, so the rejections below are the floor at work, not the runner failing. In
/// <c>...Tests.Certification</c> so it rides cert-gate.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class SharedAdaptationGovernanceTests : IDisposable
{
    private readonly string _repoRoot;
    private readonly string _sharedBase;

    public SharedAdaptationGovernanceTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "ashlar-mesh-gov-" + Guid.NewGuid().ToString("N")[..12]);
        _repoRoot = Path.Combine(baseDir, "repo");
        _sharedBase = Path.Combine(baseDir, "shared");
        Directory.CreateDirectory(_repoRoot);
        File.WriteAllText(Path.Combine(_repoRoot, "Ashlar.sln"), "# fake solution");
    }

    public void Dispose()
    {
        try { Directory.Delete(Path.GetDirectoryName(_repoRoot)!, recursive: true); } catch { /* best effort */ }
    }

    private FileBasedSharedAdaptationStore NewStore() => new(
        _sharedBase,
        new NoOpAdaptationLog(),
        new PassingRegressionRunner(),
        new PermissiveImmutableCore(),
        logger: null,
        sourcePeerId: null,
        trustedPeerIds: null,
        repoRoot: _repoRoot);

    private static SharedAdaptationEntry EntryWith(string path, string content) => new()
    {
        Id = "adapt-" + Guid.NewGuid().ToString("N")[..8],
        Record = new AdaptationRecord
        {
            Id = "rec",
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = "b",
            FailureType = "EmptyCatch",
            FixApplied = AdaptationFixType.Source,
            FilePath = path,
            RegressionPassed = true,
            Promoted = true,
            Message = "m",
        },
        Files = new Dictionary<string, byte[]> { [path] = System.Text.Encoding.UTF8.GetBytes(content) },
        BroadcastAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Adopt_BenignPath_IsWritten()
    {
        var ok = await NewStore().ValidateAndAdoptAsync(EntryWith("tests/foo.cs", "// fixed"));

        ok.Should().BeTrue("a benign path under the repo, passing regression, must be adopted");
        File.ReadAllText(Path.Combine(_repoRoot, "tests", "foo.cs")).Should().Be("// fixed");
    }

    [Theory]
    [InlineData("ashlar.policy.yaml")]
    [InlineData("ashlar.yaml")]
    [InlineData(".ashlar/gates/forged.json")]
    [InlineData("Directory.Build.targets")]
    [InlineData("Directory.Solution.targets")]
    [InlineData("src/Evil.csproj")]
    [InlineData("global.json")]
    public async Task Adopt_GovernanceOrBuildPath_IsRejected_AndNotWritten(string path)
    {
        var ok = await NewStore().ValidateAndAdoptAsync(EntryWith(path, "pwned"));

        ok.Should().BeFalse($"'{path}' is a governance/build path and must be refused");
        File.Exists(Path.Combine(_repoRoot, path)).Should().BeFalse("nothing may be written for a refused adoption");
    }

    [Fact]
    public async Task Adopt_PathEscapingTheRepo_IsRejected_AndNothingIsWrittenOutside()
    {
        var marker = "pwned-" + Guid.NewGuid().ToString("N")[..12] + ".txt";
        var ok = await NewStore().ValidateAndAdoptAsync(EntryWith("../" + marker, "pwned"));

        ok.Should().BeFalse("a path escaping the repo root must be refused");
        File.Exists(Path.Combine(Path.GetDirectoryName(_repoRoot)!, marker))
            .Should().BeFalse("the escape must never have been written outside the repo");
    }

    [Fact]
    public async Task Adopt_OneBadFileInABatch_RejectsAll_BeforeAnyWrite()
    {
        var entry = new SharedAdaptationEntry
        {
            Id = "adapt-batch",
            Record = EntryWith("tests/good.cs", "ok").Record,
            Files = new Dictionary<string, byte[]>
            {
                ["tests/good.cs"] = System.Text.Encoding.UTF8.GetBytes("ok"),
                ["Directory.Build.props"] = System.Text.Encoding.UTF8.GetBytes("<Project/>"),
            },
            BroadcastAt = DateTimeOffset.UtcNow,
        };

        var ok = await NewStore().ValidateAndAdoptAsync(entry);

        ok.Should().BeFalse();
        File.Exists(Path.Combine(_repoRoot, "tests", "good.cs"))
            .Should().BeFalse("the whole entry is validated before any file is written");
    }

    private sealed class NoOpAdaptationLog : IAdaptationLog
    {
        public Task LogAsync(AdaptationRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AdaptationRecord>> QueryAsync(
            DateTimeOffset? since = null, DateTimeOffset? until = null, string? brickId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AdaptationRecord>>(Array.Empty<AdaptationRecord>());
    }

    private sealed class PassingRegressionRunner : IRegressionTestRunner
    {
        public Task<RegressionTestResult> RunAsync(string projectOrSolutionPath, string? filter = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new RegressionTestResult { AllPassed = true, PassedCount = 1, FailedCount = 0, Summary = "stub" });
    }

    private sealed class PermissiveImmutableCore : IImmutableCoreRegistry
    {
        public IReadOnlyList<string> CoreComponentIds => Array.Empty<string>();
        public bool IsInImmutableCore(string pathOrComponentId) => false;
        public bool IsCoreNamespace(string namespaceOrPath) => false;
    }
}

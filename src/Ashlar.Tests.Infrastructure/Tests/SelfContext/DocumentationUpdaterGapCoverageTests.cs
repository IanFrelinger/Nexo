using FluentAssertions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure.SelfContext;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SelfContext;

/// <summary>
/// Tests for documentation updater gap coverage.
///
/// <para>Every test here passes an explicit temporary docs root. They used to write under
/// <c>RepoPathResolver.FindRepoRoot()</c> and tidy up in a <c>finally</c>, which mutates tracked
/// files in the developer's working tree for the duration of the run — and leaves them mutated
/// for good if the run is killed before the cleanup, as a cancelled CI job, a Ctrl+C or a stopped
/// container all do. That is how <c>docs/bricks/unknown.md</c> came to be modified with no obvious
/// author. A temp root makes the failure mode impossible rather than merely tidied up after.</para>
/// </summary>
public sealed class DocumentationUpdaterGapCoverageTests : IDisposable
{
    private readonly string _docsRoot =
        Path.Combine(Path.GetTempPath(), "ashlar-docs-" + Guid.NewGuid().ToString("N"));

    private string BrickDoc(string id) => Path.Combine(_docsRoot, "docs", "bricks", $"{id}.md");

    public void Dispose()
    {
        if (Directory.Exists(_docsRoot))
            Directory.Delete(_docsRoot, recursive: true);
    }

    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var log = Mock.Of<IAdaptationLog>();
        var changelog = Mock.Of<IChangelogGenerator>();

        var act = () => new DocumentationUpdater(null!, changelog);
        act.Should().Throw<ArgumentNullException>().WithParameterName("adaptationLog");

        act = () => new DocumentationUpdater(log, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("changelogGenerator");
    }

    [Fact]
    public async Task UpdateForAdaptationAsync_is_no_op_when_record_is_not_promoted()
    {
        var adaptationId = "gap-non-promoted-" + Guid.NewGuid().ToString("N");
        var updater = new DocumentationUpdater(
            LogReturning(adaptationId, brickId: "gap-brick", promoted: false),
            Mock.Of<IChangelogGenerator>(),
            _docsRoot);

        await updater.UpdateForAdaptationAsync(adaptationId);

        File.Exists(BrickDoc("gap-brick")).Should().BeFalse(
            "an unpromoted adaptation has not been accepted, so it must not be documented as though it had");
    }

    [Fact]
    public async Task UpdateForAdaptationAsync_writes_the_brick_document_when_promoted()
    {
        var adaptationId = "gap-promoted-" + Guid.NewGuid().ToString("N");
        var updater = new DocumentationUpdater(
            LogReturning(adaptationId, brickId: "gap-brick", promoted: true),
            Mock.Of<IChangelogGenerator>(),
            _docsRoot);

        await updater.UpdateForAdaptationAsync(adaptationId);

        var written = await File.ReadAllTextAsync(BrickDoc("gap-brick"));
        written.Should().Contain(adaptationId, "the document records which promotion produced it");
    }

    /// <summary>
    /// A promoted record with no brick id lands in <c>unknown.md</c> — a single shared filename,
    /// rewritten by every such promotion. Pinned because it is the reason a tracked file kept
    /// turning up modified: the name carries no run identity, so the write is invisible in a
    /// diff except as a changed date and promotion id.
    /// </summary>
    [Fact]
    public async Task UpdateForAdaptationAsync_falls_back_to_unknown_when_the_record_has_no_brick_id()
    {
        var adaptationId = "gap-no-brick-" + Guid.NewGuid().ToString("N");
        var updater = new DocumentationUpdater(
            LogReturning(adaptationId, brickId: null, promoted: true),
            Mock.Of<IChangelogGenerator>(),
            _docsRoot);

        await updater.UpdateForAdaptationAsync(adaptationId);

        File.Exists(BrickDoc("unknown")).Should().BeTrue();
        (await File.ReadAllTextAsync(BrickDoc("unknown"))).Should().Contain(adaptationId);
    }

    [Fact]
    public async Task GenerateStubAsync_skips_when_documentation_already_exists()
    {
        var componentId = "gap-stub-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.GetDirectoryName(BrickDoc(componentId))!);
        await File.WriteAllTextAsync(BrickDoc(componentId), "# existing");

        var updater = new DocumentationUpdater(
            Mock.Of<IAdaptationLog>(), Mock.Of<IChangelogGenerator>(), _docsRoot);
        await updater.GenerateStubAsync(componentId);

        (await File.ReadAllTextAsync(BrickDoc(componentId))).Should().Be(
            "# existing", "a stub must never overwrite real documentation");
    }

    [Fact]
    public async Task GenerateStubAsync_creates_markdown_when_missing()
    {
        var componentId = "gap-stub-new-" + Guid.NewGuid().ToString("N");

        var updater = new DocumentationUpdater(
            Mock.Of<IAdaptationLog>(), Mock.Of<IChangelogGenerator>(), _docsRoot);
        await updater.GenerateStubAsync(componentId);

        File.Exists(BrickDoc(componentId)).Should().BeTrue();
        (await File.ReadAllTextAsync(BrickDoc(componentId))).Should().Contain(componentId);
    }

    private static IAdaptationLog LogReturning(string adaptationId, string? brickId, bool promoted)
    {
        var log = new Mock<IAdaptationLog>();
        log.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AdaptationRecord
                {
                    Id = adaptationId,
                    Timestamp = DateTimeOffset.UtcNow,
                    BrickId = brickId,
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Source,
                    Promoted = promoted,
                },
            ]);
        return log.Object;
    }
}

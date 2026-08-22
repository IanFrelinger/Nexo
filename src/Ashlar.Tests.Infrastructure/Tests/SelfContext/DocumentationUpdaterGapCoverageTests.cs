using FluentAssertions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure.SelfContext;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SelfContext;

/// <summary>Tests for documentation updater gap coverage.</summary>
public sealed class DocumentationUpdaterGapCoverageTests
{
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
        var log = new Mock<IAdaptationLog>();
        log.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AdaptationRecord
                {
                    Id = adaptationId,
                    Timestamp = DateTimeOffset.UtcNow,
                    BrickId = "gap-brick",
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Source,
                    Promoted = false,
                },
            ]);

        var updater = new DocumentationUpdater(log.Object, Mock.Of<IChangelogGenerator>());
        var docPath = Path.Combine(RepoPathResolver.FindRepoRoot(), "docs", "bricks", "gap-brick.md");

        if (File.Exists(docPath))
            File.Delete(docPath);

        await updater.UpdateForAdaptationAsync(adaptationId);

        File.Exists(docPath).Should().BeFalse();
    }

    [Fact]
    public async Task GenerateStubAsync_skips_when_documentation_already_exists()
    {
        var componentId = "gap-stub-" + Guid.NewGuid().ToString("N");
        var docPath = Path.Combine(RepoPathResolver.FindRepoRoot(), "docs", "bricks", $"{componentId}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(docPath)!);
        await File.WriteAllTextAsync(docPath, "# existing");

        try
        {
            var updater = new DocumentationUpdater(Mock.Of<IAdaptationLog>(), Mock.Of<IChangelogGenerator>());
            await updater.GenerateStubAsync(componentId);

            (await File.ReadAllTextAsync(docPath)).Should().Be("# existing");
        }
        finally
        {
            if (File.Exists(docPath))
                File.Delete(docPath);
        }
    }

    [Fact]
    public async Task GenerateStubAsync_creates_markdown_when_missing()
    {
        var componentId = "gap-stub-new-" + Guid.NewGuid().ToString("N");
        var docPath = Path.Combine(RepoPathResolver.FindRepoRoot(), "docs", "bricks", $"{componentId}.md");

        if (File.Exists(docPath))
            File.Delete(docPath);

        try
        {
            var updater = new DocumentationUpdater(Mock.Of<IAdaptationLog>(), Mock.Of<IChangelogGenerator>());
            await updater.GenerateStubAsync(componentId);

            File.Exists(docPath).Should().BeTrue();
            (await File.ReadAllTextAsync(docPath)).Should().Contain(componentId);
        }
        finally
        {
            if (File.Exists(docPath))
                File.Delete(docPath);
        }
    }
}

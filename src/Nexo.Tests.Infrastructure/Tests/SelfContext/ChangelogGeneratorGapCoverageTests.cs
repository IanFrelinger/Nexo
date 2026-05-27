using FluentAssertions;
using Moq;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Infrastructure.SelfContext;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SelfContext;

public sealed class ChangelogGeneratorGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_adaptation_log()
    {
        var act = () => new ChangelogGenerator(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("adaptationLog");
    }

    [Fact]
    public async Task GenerateAsync_returns_empty_message_when_no_promoted_records()
    {
        var log = new Mock<IAdaptationLog>();
        log.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AdaptationRecord
                {
                    Id = "a1",
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Source,
                    Promoted = false,
                },
            ]);

        var generator = new ChangelogGenerator(log.Object);
        var markdown = await generator.GenerateAsync();

        markdown.Should().Contain("No promoted changes");
    }

    [Fact]
    public async Task GenerateAsync_lists_promoted_records_in_descending_timestamp_order()
    {
        var older = DateTimeOffset.UtcNow.AddHours(-2);
        var newer = DateTimeOffset.UtcNow.AddHours(-1);
        var log = new Mock<IAdaptationLog>();
        log.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AdaptationRecord
                {
                    Id = "old",
                    Timestamp = older,
                    BrickId = "brick-old",
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Source,
                    Message = "older fix",
                    Promoted = true,
                },
                new AdaptationRecord
                {
                    Id = "new",
                    Timestamp = newer,
                    BrickId = "brick-new",
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Source,
                    Message = "newer fix",
                    Promoted = true,
                },
            ]);

        var generator = new ChangelogGenerator(log.Object);
        var markdown = await generator.GenerateAsync(since: older.AddMinutes(-1), until: newer.AddMinutes(1));

        markdown.IndexOf("brick-new", StringComparison.Ordinal)
            .Should()
            .BeLessThan(markdown.IndexOf("brick-old", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_honors_cancellation()
    {
        var log = new Mock<IAdaptationLog>();
        var generator = new ChangelogGenerator(log.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await generator.GenerateAsync(cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

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

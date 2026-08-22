using FluentAssertions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure.SelfContext;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SelfContext;

/// <summary>Tests for changelog generator gap coverage.</summary>
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

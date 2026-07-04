using FluentAssertions;
using Moq;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Knowledge.Models;
using Nexo.Core.Application.Knowledge.Ports;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Knowledge;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Knowledge;

/// <summary>Tests for knowledge query service.</summary>
public sealed class KnowledgeQueryServiceTests
{
    [Fact]
    public async Task QueryAsync_aggregates_all_sources_with_filters_and_pagination()
    {
        var adaptationLog = new Mock<IAdaptationLog>();
        adaptationLog.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new AdaptationRecord
                {
                    Id = "a1",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5),
                    FailureType = "lint",
                    FilePath = "/src/Foo.cs",
                    BrickId = "brick-a",
                    FixApplied = AdaptationFixType.Source,
                    Promoted = true,
                },
            });

        var patternStore = new Mock<IPatternStore>();
        patternStore.Setup(s => s.QueryAsync(It.IsAny<PatternStoreQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ObservedPattern
                {
                    PatternId = "p1",
                    EventType = "pattern",
                    Frequency = 3,
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
                    LastSeen = DateTimeOffset.UtcNow.AddMinutes(-1),
                    RelatedEventIds = ["evt-1", "evt-2"],
                },
            });

        var knowledgeStore = new Mock<IUserKnowledgeLogStore>();
        knowledgeStore.Setup(s => s.GetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new UserKnowledgeLogEntry
                {
                    Id = "k1",
                    DataType = "lint",
                    Content = "prefer var",
                    UpdatedAt = DateTimeOffset.UtcNow,
                    SourceObservationIds = ["obs-1"],
                },
            });

        var sut = new KnowledgeQueryService(adaptationLog.Object, patternStore.Object, knowledgeStore.Object);
        var result = await sut.QueryAsync(new KnowledgeQueryRequest
        {
            DataType = "lint",
            MaxCount = 2,
            Offset = 0,
        });

        result.Total.Should().BeGreaterThanOrEqualTo(2);
        result.Entries.Should().HaveCountLessThanOrEqualTo(2);
        result.Entries.Should().OnlyContain(e => e.DataType == "lint");
    }

    [Fact]
    public async Task QueryAsync_returns_partial_results_when_one_store_throws()
    {
        var adaptationLog = new Mock<IAdaptationLog>();
        adaptationLog.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("adaptation down"));

        var patternStore = new Mock<IPatternStore>();
        patternStore.Setup(s => s.QueryAsync(It.IsAny<PatternStoreQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ObservedPattern
                {
                    PatternId = "p-only",
                    EventType = "build",
                    Frequency = 1,
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
                    LastSeen = DateTimeOffset.UtcNow,
                },
            });

        var knowledgeStore = new Mock<IUserKnowledgeLogStore>();
        knowledgeStore.Setup(s => s.GetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("knowledge down"));

        var sut = new KnowledgeQueryService(adaptationLog.Object, patternStore.Object, knowledgeStore.Object);
        var result = await sut.QueryAsync(new KnowledgeQueryRequest
        {
            Sources = [KnowledgeSource.Pattern],
            EventType = "pattern",
        });

        result.Entries.Should().ContainSingle(e => e.Id == "p-only");
    }

    [Fact]
    public async Task QueryAsync_clamps_max_count_and_offset()
    {
        var adaptationLog = new Mock<IAdaptationLog>();
        adaptationLog.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AdaptationRecord>());

        var patternStore = new Mock<IPatternStore>();
        patternStore.Setup(s => s.QueryAsync(It.IsAny<PatternStoreQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ObservedPattern>());

        var knowledgeStore = new Mock<IUserKnowledgeLogStore>();
        knowledgeStore.Setup(s => s.GetAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserKnowledgeLogEntry>());

        var sut = new KnowledgeQueryService(adaptationLog.Object, patternStore.Object, knowledgeStore.Object);
        var result = await sut.QueryAsync(new KnowledgeQueryRequest { MaxCount = 0, Offset = -3 });

        result.Entries.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var adaptation = Mock.Of<IAdaptationLog>();
        var patterns = Mock.Of<IPatternStore>();
        var knowledge = Mock.Of<IUserKnowledgeLogStore>();

        /// <summary>Action.</summary>
        new Action(() => new KnowledgeQueryService(null!, patterns, knowledge))
            .Should().Throw<ArgumentNullException>();
        /// <summary>Action.</summary>
        new Action(() => new KnowledgeQueryService(adaptation, null!, knowledge))
            .Should().Throw<ArgumentNullException>();
        /// <summary>Action.</summary>
        new Action(() => new KnowledgeQueryService(adaptation, patterns, null!))
            .Should().Throw<ArgumentNullException>();
    }
}

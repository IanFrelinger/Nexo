using FluentAssertions;
using Moq;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for mesh knowledge export service gap coverage.</summary>
public sealed class MeshKnowledgeExportServiceGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var adaptation = Mock.Of<IAdaptationLog>();
        var patterns = Mock.Of<IPatternStore>();

        var act = () => new MeshKnowledgeExportService(null!, patterns);
        act.Should().Throw<ArgumentNullException>().WithParameterName("adaptationLog");

        act = () => new MeshKnowledgeExportService(adaptation, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("patternStore");
    }

    [Fact]
    public async Task ExportAsync_clamps_limits_and_orders_adaptations_by_timestamp()
    {
        var older = new AdaptationRecord
        {
            Id = "old",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            FailureType = "compile",
            FixApplied = AdaptationFixType.Source,
        };
        var newer = new AdaptationRecord
        {
            Id = "new",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            FailureType = "test",
            FixApplied = AdaptationFixType.Brick,
        };

        var adaptation = new Mock<IAdaptationLog>();
        DateTimeOffset? capturedSince = null;
        adaptation.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset?, DateTimeOffset?, string?, CancellationToken>((since, _, _, _) => capturedSince = since)
            .ReturnsAsync([older, newer]);

        var patterns = new Mock<IPatternStore>();
        patterns.Setup(p => p.QueryAsync(It.IsAny<PatternStoreQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ObservedPattern
                {
                    PatternId = "p1",
                    EventType = "edit",
                    Frequency = 1,
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
                    LastSeen = DateTimeOffset.UtcNow,
                },
            ]);

        var service = new MeshKnowledgeExportService(adaptation.Object, patterns.Object);
        var since = DateTimeOffset.UtcNow.AddDays(-1);

        var payload = await service.ExportAsync(since, maxAdaptations: 1, maxPatterns: 0);

        capturedSince.Should().Be(since);
        payload.Adaptations.Should().ContainSingle().Which.Id.Should().Be("new");
        payload.Patterns.Should().ContainSingle();
        payload.ExportedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExportAsync_clamps_non_positive_limits_to_one()
    {
        var adaptation = new Mock<IAdaptationLog>();
        adaptation.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AdaptationRecord
                {
                    Id = "a1",
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureType = "compile",
                    FixApplied = AdaptationFixType.Source,
                },
                new AdaptationRecord
                {
                    Id = "a2",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1),
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Brick,
                },
            ]);

        var patterns = new Mock<IPatternStore>();
        patterns.Setup(p => p.QueryAsync(It.IsAny<PatternStoreQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ObservedPattern
                {
                    PatternId = "p1",
                    EventType = "edit",
                    Frequency = 1,
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
                    LastSeen = DateTimeOffset.UtcNow,
                },
                new ObservedPattern
                {
                    PatternId = "p2",
                    EventType = "build",
                    Frequency = 2,
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-2),
                    LastSeen = DateTimeOffset.UtcNow,
                },
            ]);

        var service = new MeshKnowledgeExportService(adaptation.Object, patterns.Object);

        var payload = await service.ExportAsync(DateTimeOffset.UtcNow.AddDays(-1), maxAdaptations: 0, maxPatterns: -5);

        payload.Adaptations.Should().ContainSingle();
        payload.Patterns.Should().HaveCount(2);
        patterns.Verify(p => p.QueryAsync(
            It.Is<PatternStoreQueryParams>(q => q.MaxCount == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

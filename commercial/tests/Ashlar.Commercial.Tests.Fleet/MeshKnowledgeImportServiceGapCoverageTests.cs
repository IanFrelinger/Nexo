using FluentAssertions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for mesh knowledge import service gap coverage.</summary>
public sealed class MeshKnowledgeImportServiceGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var adaptation = Mock.Of<IAdaptationLog>();
        var patterns = Mock.Of<IPatternStore>();

        var act = () => new MeshKnowledgeImportService(null!, patterns);
        act.Should().Throw<ArgumentNullException>().WithParameterName("adaptationLog");

        act = () => new MeshKnowledgeImportService(adaptation, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("patternStore");
    }

    [Fact]
    public async Task ImportAsync_counts_applied_and_skipped_records()
    {
        var adaptation = new Mock<IAdaptationLog>();
        adaptation.SetupSequence(l => l.LogAsync(It.IsAny<AdaptationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new LiteException("duplicate key entry"));

        var patterns = new Mock<IPatternStore>();
        patterns.SetupSequence(s => s.AddAsync(It.IsAny<ObservedPattern>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store unavailable"))
            .Returns(Task.CompletedTask);

        var service = new MeshKnowledgeImportService(adaptation.Object, patterns.Object);
        var payload = new MeshKnowledgeExportPayload(
            DateTimeOffset.UtcNow,
            [
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
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureType = "test",
                    FixApplied = AdaptationFixType.Brick,
                },
            ],
            [
                new ObservedPattern
                {
                    PatternId = "p1",
                    EventType = "edit",
                    Frequency = 1,
                    FirstSeen = DateTimeOffset.UtcNow.AddMinutes(-1),
                    LastSeen = DateTimeOffset.UtcNow,
                },
                new ObservedPattern
                {
                    PatternId = "p2",
                    EventType = "build",
                    Frequency = 2,
                    FirstSeen = DateTimeOffset.UtcNow.AddMinutes(-2),
                    LastSeen = DateTimeOffset.UtcNow,
                },
            ]);

        var result = await service.ImportAsync(payload);

        result.AdaptationsApplied.Should().Be(1);
        result.AdaptationsSkipped.Should().Be(1);
        result.PatternsApplied.Should().Be(1);
        result.PatternsSkipped.Should().Be(1);
    }

    /// <summary>Exception thrown when lite fails.</summary>
    private sealed class LiteException(string message) : Exception(message);
}

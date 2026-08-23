using FluentAssertions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.SelfContext.Models;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure.SelfContext;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SelfContext;

/// <summary>Tests for self context assembler gap coverage.</summary>
public sealed class SelfContextAssemblerGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var adaptation = Mock.Of<IAdaptationLog>();
        var tracer = Mock.Of<IExecutionTracer>();
        var patterns = Mock.Of<IPatternStore>();

        var act = () => new SelfContextAssembler(null!, tracer, patterns);
        act.Should().Throw<ArgumentNullException>().WithParameterName("adaptationLog");

        act = () => new SelfContextAssembler(adaptation, null!, patterns);
        act.Should().Throw<ArgumentNullException>().WithParameterName("executionTracer");

        act = () => new SelfContextAssembler(adaptation, tracer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("patternStore");
    }

    [Fact]
    public async Task AssembleAsync_aggregates_sources_and_builds_summary()
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
                    FilePath = "src/Foo.cs",
                    RegressionPassed = true,
                    Promoted = true,
                },
            ]);

        var tracer = new Mock<IExecutionTracer>();
        tracer.Setup(t => t.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ExecutionTraceEntry
                {
                    Id = "e1",
                    Timestamp = DateTimeOffset.UtcNow,
                    Operation = "build",
                    Path = "src/Foo.cs",
                    Outcome = "success",
                },
            ]);

        var patterns = new Mock<IPatternStore>();
        patterns.Setup(p => p.QueryAsync(It.IsAny<PatternStoreQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ObservedPattern
                {
                    PatternId = "p1",
                    EventType = "build-failure",
                    Frequency = 2,
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-1),
                    LastSeen = DateTimeOffset.UtcNow,
                },
            ]);

        var assembler = new SelfContextAssembler(adaptation.Object, tracer.Object, patterns.Object);

        var model = await assembler.AssembleAsync(TimeSpan.FromHours(24));

        model.RecentAdaptations.Should().ContainSingle();
        model.RecentExecutions.Should().ContainSingle();
        model.RecentPatterns.Should().ContainSingle();
        model.Summary.Should().Contain("Recent Activity Summary");
        model.Summary.Should().Contain("Adaptations (1)");
        model.Summary.Should().Contain("Executions (1)");
        model.Summary.Should().Contain("Patterns (1)");
        model.Summary.Should().Contain("promoted");
    }

    [Fact]
    public async Task AssembleAsync_without_lookback_queries_all_history()
    {
        DateTimeOffset? capturedSince = DateTimeOffset.MinValue;
        var adaptation = new Mock<IAdaptationLog>();
        adaptation.Setup(l => l.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), null, It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset?, DateTimeOffset?, string?, CancellationToken>((since, _, _, _) => capturedSince = since)
            .ReturnsAsync(Array.Empty<AdaptationRecord>());

        var tracer = new Mock<IExecutionTracer>();
        tracer.Setup(t => t.QueryAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExecutionTraceEntry>());

        var patterns = new Mock<IPatternStore>();
        patterns.Setup(p => p.QueryAsync(It.IsAny<PatternStoreQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ObservedPattern>());

        var assembler = new SelfContextAssembler(adaptation.Object, tracer.Object, patterns.Object);

        await assembler.AssembleAsync();

        capturedSince.Should().BeNull();
    }
}

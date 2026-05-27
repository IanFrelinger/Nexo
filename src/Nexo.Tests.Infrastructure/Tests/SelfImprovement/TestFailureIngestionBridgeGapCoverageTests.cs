using FluentAssertions;
using Nexo.Core.Application.Common.Models;
using Moq;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Infrastructure.SelfImprovement;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SelfImprovement;

public sealed class TestFailureIngestionBridgeGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_store()
    {
        var act = () => new TestFailureIngestionBridge(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public async Task IngestAsync_returns_zero_for_all_passing_results()
    {
        var store = new Mock<ITestFailureStore>();
        var bridge = new TestFailureIngestionBridge(store.Object);

        var count = await bridge.IngestAsync([
            new TestResult { Name = "passing", Passed = true },
        ]);

        count.Should().Be(0);
        store.Verify(s => s.RecordAsync(It.IsAny<TestFailureRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_records_only_failures_and_uses_message_fallback()
    {
        TestFailureRecord? captured = null;
        var store = new Mock<ITestFailureStore>();
        store.Setup(s => s.RecordAsync(It.IsAny<TestFailureRecord>(), It.IsAny<CancellationToken>()))
            .Callback<TestFailureRecord, CancellationToken>((record, _) => captured = record)
            .Returns(Task.CompletedTask);

        var bridge = new TestFailureIngestionBridge(store.Object);
        var count = await bridge.IngestAsync([
            new TestResult { Name = "passing", Passed = true },
            new TestResult
            {
                Name = "failing",
                Passed = false,
                Message = "expected true",
                StackTrace = "at Fail()",
            },
        ]);

        count.Should().Be(1);
        captured.Should().NotBeNull();
        captured!.TestName.Should().Be("failing");
        captured.ErrorMessage.Should().Be("expected true");
        captured.StackTrace.Should().Be("at Fail()");
    }
}

public sealed class FileBasedSelfImprovementMetricsStoreGapCoverageTests
{
    [Fact]
    public async Task GetLastAsync_returns_null_when_file_missing()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-metrics-" + Guid.NewGuid().ToString("N") + ".json");
        var store = new FileBasedSelfImprovementMetricsStore(path);

        (await store.GetLastAsync()).Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_and_GetLastAsync_round_trip_report()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexo-metrics-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new FileBasedSelfImprovementMetricsStore(path);
            var report = new Nexo.Core.Application.SelfImprovement.Models.SelfImprovementReport(
                RunAt: DateTimeOffset.UtcNow,
                FailuresProcessed: 2,
                FixesGenerated: 1,
                FixesValidated: 1,
                FixesPromoted: 1,
                FixesRejected: 0,
                PromotedAdaptationIds: ["a1"],
                RejectedReasons: []);

            await store.SaveAsync(report);

            var loaded = await store.GetLastAsync();
            loaded.Should().NotBeNull();
            loaded!.FailuresProcessed.Should().Be(2);
            loaded.FixesPromoted.Should().Be(1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

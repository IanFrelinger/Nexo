using FluentAssertions;
using Ashlar.Core.Application.Common.Models;
using Moq;
using Ashlar.Core.Application.SelfContext.Models;
using Ashlar.Core.Application.SelfContext.Ports;
using Ashlar.Infrastructure.SelfImprovement;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.SelfImprovement;

/// <summary>Tests for test failure ingestion bridge gap coverage.</summary>
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

using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Metrics;

namespace Nexo.Tests.Infrastructure.Tests.Metrics;

public class MemoryMetricsCollectorTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestRecordExecutionTime();
            await TestRecordExecutionTimeMultiple();
            await TestIncrementCounter();
            await TestIncrementCounterMultiple();
            await TestGetSnapshotAsync();
            await TestConcurrentAccess();

            return new TestResult
            {
                TestName = nameof(MemoryMetricsCollectorTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All MemoryMetricsCollector tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(MemoryMetricsCollectorTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(MemoryMetricsCollectorTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private Task TestRecordExecutionTime()
    {
        var mockLogger = new Mock<ILogger<MemoryMetricsCollector>>();
        var collector = new MemoryMetricsCollector(mockLogger.Object);

        collector.RecordExecutionTime("test-operation", TimeSpan.FromMilliseconds(100));

        var snapshot = collector.GetSnapshotAsync().Result;
        AssertTrue(snapshot.ExecutionTimes.ContainsKey("test-operation"));
        AssertEqual(TimeSpan.FromMilliseconds(100), snapshot.ExecutionTimes["test-operation"]);

        return Task.CompletedTask;
    }

    private Task TestRecordExecutionTimeMultiple()
    {
        var mockLogger = new Mock<ILogger<MemoryMetricsCollector>>();
        var collector = new MemoryMetricsCollector(mockLogger.Object);

        collector.RecordExecutionTime("test-operation", TimeSpan.FromMilliseconds(50));
        collector.RecordExecutionTime("test-operation", TimeSpan.FromMilliseconds(75));

        var snapshot = collector.GetSnapshotAsync().Result;
        AssertTrue(snapshot.ExecutionTimes.ContainsKey("test-operation"));
        AssertEqual(TimeSpan.FromMilliseconds(125), snapshot.ExecutionTimes["test-operation"]);

        return Task.CompletedTask;
    }

    private Task TestIncrementCounter()
    {
        var mockLogger = new Mock<ILogger<MemoryMetricsCollector>>();
        var collector = new MemoryMetricsCollector(mockLogger.Object);

        collector.IncrementCounter("test-counter");

        var snapshot = collector.GetSnapshotAsync().Result;
        AssertTrue(snapshot.Counters.ContainsKey("test-counter"));
        AssertEqual(1L, snapshot.Counters["test-counter"]);

        return Task.CompletedTask;
    }

    private Task TestIncrementCounterMultiple()
    {
        var mockLogger = new Mock<ILogger<MemoryMetricsCollector>>();
        var collector = new MemoryMetricsCollector(mockLogger.Object);

        collector.IncrementCounter("test-counter", 5);
        collector.IncrementCounter("test-counter", 3);

        var snapshot = collector.GetSnapshotAsync().Result;
        AssertTrue(snapshot.Counters.ContainsKey("test-counter"));
        AssertEqual(8L, snapshot.Counters["test-counter"]);

        return Task.CompletedTask;
    }

    private Task TestGetSnapshotAsync()
    {
        var mockLogger = new Mock<ILogger<MemoryMetricsCollector>>();
        var collector = new MemoryMetricsCollector(mockLogger.Object);

        collector.RecordExecutionTime("op1", TimeSpan.FromMilliseconds(100));
        collector.RecordExecutionTime("op2", TimeSpan.FromMilliseconds(200));
        collector.IncrementCounter("counter1", 10);
        collector.IncrementCounter("counter2", 20);

        var snapshot = collector.GetSnapshotAsync().Result;

        AssertNotNull(snapshot);
        AssertEqual(2, snapshot.ExecutionTimes.Count);
        AssertEqual(2, snapshot.Counters.Count);
        AssertTrue(snapshot.ExecutionTimes.ContainsKey("op1"));
        AssertTrue(snapshot.ExecutionTimes.ContainsKey("op2"));
        AssertTrue(snapshot.Counters.ContainsKey("counter1"));
        AssertTrue(snapshot.Counters.ContainsKey("counter2"));
        AssertEqual(10L, snapshot.Counters["counter1"]);
        AssertEqual(20L, snapshot.Counters["counter2"]);

        return Task.CompletedTask;
    }

    private Task TestConcurrentAccess()
    {
        var mockLogger = new Mock<ILogger<MemoryMetricsCollector>>();
        var collector = new MemoryMetricsCollector(mockLogger.Object);

        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
            {
                collector.RecordExecutionTime($"op{index}", TimeSpan.FromMilliseconds(index * 10));
                collector.IncrementCounter($"counter{index}", index);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        var snapshot = collector.GetSnapshotAsync().Result;
        AssertEqual(10, snapshot.ExecutionTimes.Count);
        AssertEqual(10, snapshot.Counters.Count);

        return Task.CompletedTask;
    }
}


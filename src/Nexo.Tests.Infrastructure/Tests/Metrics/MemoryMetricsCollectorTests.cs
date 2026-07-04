using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Metrics;

namespace Nexo.Tests.Infrastructure.Tests.Metrics;

/// <summary>Tests for memory metrics collector.</summary>
public class MemoryMetricsCollectorTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test record execution time.</summary>
            await TestRecordExecutionTime();
            /// <summary>Test record execution time multiple.</summary>
            await TestRecordExecutionTimeMultiple();
            /// <summary>Test increment counter.</summary>
            await TestIncrementCounter();
            /// <summary>Test increment counter multiple.</summary>
            await TestIncrementCounterMultiple();
            /// <summary>Test get snapshot async.</summary>
            await TestGetSnapshotAsync();
            /// <summary>Test concurrent access.</summary>
            await TestConcurrentAccess();

            return new TestResult
            {
                Name = nameof(MemoryMetricsCollectorTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All MemoryMetricsCollector tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(MemoryMetricsCollectorTests),
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
                Name = nameof(MemoryMetricsCollectorTests),
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
        /// <summary>Assert equal.</summary>
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
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(snapshot);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, snapshot.ExecutionTimes.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, snapshot.Counters.Count);
        AssertTrue(snapshot.ExecutionTimes.ContainsKey("op1"));
        AssertTrue(snapshot.ExecutionTimes.ContainsKey("op2"));
        AssertTrue(snapshot.Counters.ContainsKey("counter1"));
        AssertTrue(snapshot.Counters.ContainsKey("counter2"));
        /// <summary>Assert equal.</summary>
        AssertEqual(10L, snapshot.Counters["counter1"]);
        /// <summary>Assert equal.</summary>
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
        /// <summary>Assert equal.</summary>
        AssertEqual(10, snapshot.ExecutionTimes.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual(10, snapshot.Counters.Count);

        return Task.CompletedTask;
    }
}


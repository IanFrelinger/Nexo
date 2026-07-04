using FluentAssertions;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Infrastructure.SelfContext;
using Nexo.Infrastructure.SelfImprovement;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.SelfImprovement;

/// <summary>Tests for test failure ingestion bridge.</summary>
[Trait("Category", "Integration")]
public sealed class TestFailureIngestionBridgeTests : TempDirTestBase
{
    private readonly string _dbPath;

    public TestFailureIngestionBridgeTests() : base("nexo-ingestion-bridge")
    {
        _dbPath = Path.Combine(TempDir, "failures.db");
    }

    [Fact]
    public async Task IngestAsync_OnlyRecordsFailures()
    {
        var store = new LiteDbTestFailureStore(_dbPath);
        var bridge = new TestFailureIngestionBridge(store);

        var results = new List<TestResult>
        {
            new() { Name = "PassingTest", Passed = true },
            new() { Name = "FailingTest1", Passed = false, ErrorMessage = "assert failed" },
            new() { Name = "AnotherPass", Passed = true },
            new() { Name = "FailingTest2", Passed = false, ErrorMessage = "timeout" },
        };

        var count = await bridge.IngestAsync(results);

        count.Should().Be(2);
        var stored = await store.QueryAsync();
        stored.Should().HaveCount(2);
        stored.Select(r => r.TestName).Should().BeEquivalentTo("FailingTest1", "FailingTest2");
    }

    [Fact]
    public async Task IngestAsync_EmptyResults_ReturnsZero()
    {
        var store = new LiteDbTestFailureStore(_dbPath);
        var bridge = new TestFailureIngestionBridge(store);

        var count = await bridge.IngestAsync(Array.Empty<TestResult>());

        count.Should().Be(0);
        var stored = await store.QueryAsync();
        stored.Should().BeEmpty();
    }

    [Fact]
    public async Task IngestAsync_AllPassing_ReturnsZero()
    {
        var store = new LiteDbTestFailureStore(_dbPath);
        var bridge = new TestFailureIngestionBridge(store);

        var results = new List<TestResult>
        {
            new() { Name = "Test1", Passed = true },
            new() { Name = "Test2", Passed = true },
            new() { Name = "Test3", Passed = true },
        };

        var count = await bridge.IngestAsync(results);

        count.Should().Be(0);
        var stored = await store.QueryAsync();
        stored.Should().BeEmpty();
    }

    [Fact]
    public async Task IngestAsync_MapsFieldsCorrectly()
    {
        var store = new LiteDbTestFailureStore(_dbPath);
        var bridge = new TestFailureIngestionBridge(store);

        var results = new List<TestResult>
        {
            new()
            {
                Name = "MyNamespace.MyTest",
                Passed = false,
                ErrorMessage = "Expected true but got false",
                StackTrace = "at MyNamespace.MyTest() in MyFile.cs:line 42"
            }
        };

        var count = await bridge.IngestAsync(results);

        count.Should().Be(1);
        var stored = await store.QueryAsync();
        stored.Should().ContainSingle();
        var record = stored[0];
        record.TestName.Should().Be("MyNamespace.MyTest");
        record.ErrorMessage.Should().Be("Expected true but got false");
        record.StackTrace.Should().Be("at MyNamespace.MyTest() in MyFile.cs:line 42");
        record.Id.Should().NotBeNullOrWhiteSpace();
        record.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Metrics;
using Xunit;

namespace Nexo.Tests.Orchestration.Tests;

public sealed class OrchestrationMetricsTests
{
    private readonly OrchestrationMetrics _sut;

    public OrchestrationMetricsTests()
    {
        var logger = new Mock<ILogger<OrchestrationMetrics>>();
        _sut = new OrchestrationMetrics(logger.Object);
    }

    [Fact(Timeout = 15000)]
    public void RecordOperation_StoresMetrics()
    {
        _sut.RecordOperation("test-op", TimeSpan.FromMilliseconds(100));

        var report = _sut.GetPerformanceReport();
        report.Operations.Should().HaveCount(1);
        report.Operations[0].OperationName.Should().Be("test-op");
        report.Operations[0].Count.Should().Be(1);
        report.Operations[0].TotalDuration.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact(Timeout = 15000)]
    public void RecordOperation_MultipleCalls_AggregatesCorrectly()
    {
        _sut.RecordOperation("op", TimeSpan.FromMilliseconds(100));
        _sut.RecordOperation("op", TimeSpan.FromMilliseconds(200));
        _sut.RecordOperation("op", TimeSpan.FromMilliseconds(50));

        var report = _sut.GetPerformanceReport();
        var op = report.Operations.Single();
        op.Count.Should().Be(3);
        op.TotalDuration.Should().Be(TimeSpan.FromMilliseconds(350));
        op.MinDuration.Should().Be(TimeSpan.FromMilliseconds(50));
        op.MaxDuration.Should().Be(TimeSpan.FromMilliseconds(200));
        op.AverageDuration.TotalMilliseconds.Should().BeApproximately(350.0 / 3, 0.01);
    }

    [Fact(Timeout = 15000)]
    public void RecordAgentExecution_StoresAndRetrieves()
    {
        _sut.RecordAgentExecution(
            "agent-1", "code-gen",
            TimeSpan.FromSeconds(5),
            AgentState.Completed,
            resourceUsageMB: 256,
            contextTokensUsed: 2000);

        var metrics = _sut.GetAgentMetrics("agent-1");
        metrics.Should().NotBeNull();
        metrics!.AgentId.Should().Be("agent-1");
        metrics.Domain.Should().Be("code-gen");
        metrics.ExecutionCount.Should().Be(1);
        metrics.LastState.Should().Be(AgentState.Completed);
        metrics.TotalResourceUsageMB.Should().Be(256);
        metrics.TotalContextTokensUsed.Should().Be(2000);
    }

    [Fact(Timeout = 15000)]
    public void GetAgentMetrics_NonExistent_ReturnsNull()
    {
        _sut.GetAgentMetrics("does-not-exist").Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void StartSpan_EndSpan_RecordsTrace()
    {
        var spanId = _sut.StartSpan("my-op", tags: new Dictionary<string, string>
        {
            ["correlationId"] = "corr-1"
        });

        _sut.EndSpan(spanId, success: true);

        var report = _sut.GetPerformanceReport();
        report.Traces.Should().HaveCount(1);
        report.Traces[0].OperationName.Should().Be("my-op");
        report.Traces[0].Success.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void GetTraces_FiltersByOperationName()
    {
        var s1 = _sut.StartSpan("alpha");
        var s2 = _sut.StartSpan("beta");
        _sut.EndSpan(s1);
        _sut.EndSpan(s2);

        var traces = _sut.GetTraces(operationName: "alpha");
        traces.Should().HaveCount(1);
        traces[0].OperationName.Should().Be("alpha");
    }

    [Fact(Timeout = 15000)]
    public void Clear_RemovesAllMetrics()
    {
        _sut.RecordOperation("op", TimeSpan.FromMilliseconds(10));
        _sut.RecordAgentExecution("a1", "d", TimeSpan.FromSeconds(1), AgentState.Completed);
        _sut.StartSpan("span");

        _sut.Clear();

        var report = _sut.GetPerformanceReport();
        report.Operations.Should().BeEmpty();
        report.Agents.Should().BeEmpty();
        report.Traces.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void PerformanceReport_AgentsOrderedByExecutionCount()
    {
        _sut.RecordAgentExecution("few", "d", TimeSpan.FromSeconds(1), AgentState.Completed);
        _sut.RecordAgentExecution("many", "d", TimeSpan.FromSeconds(1), AgentState.Completed);
        _sut.RecordAgentExecution("many", "d", TimeSpan.FromSeconds(1), AgentState.Completed);
        _sut.RecordAgentExecution("many", "d", TimeSpan.FromSeconds(1), AgentState.Completed);

        var report = _sut.GetPerformanceReport();
        report.Agents[0].AgentId.Should().Be("many");
        report.Agents[0].ExecutionCount.Should().Be(3);
    }
}

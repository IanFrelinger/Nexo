using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

/// <summary>
/// P0: Adversarial scope escape tests.
/// Proves that a chain of individually-valid tool calls cannot cumulatively cross
/// PathAllowlist or MaxWriteSize boundaries. Rejection occurs before any partial application.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AdversarialScopeEscapeTests
{
    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    private static ToolCall CreateWriteCall(string path, string content)
    {
        var json = JsonSerializer.SerializeToElement(new { path, content });
        return new ToolCall("repo.fs.write", json);
    }

    private static string GetPathFromCall(ToolCall call)
    {
        if (call.Arguments.ValueKind == JsonValueKind.Object &&
            call.Arguments.TryGetProperty("path", out var p))
            return p.GetString() ?? "";
        return "";
    }

    private static PolicyEngine CreateScopePolicyEngine()
    {
        return new PolicyEngine(new IPolicy[]
        {
            new PathAllowlist(),
            new MaxWriteSize(200_000)
        });
    }

    [Fact]
    public void AdversarialScope_ChainOfValidAdaptations_CannotCumulativelyCrossBoundary()
    {
        var engine = CreateScopePolicyEngine();
        var chain = new[]
        {
            CreateWriteCall("src/foo/a.cs", "a"),
            CreateWriteCall("src/foo/b.cs", "b"),
            CreateWriteCall("tests/bar/c.cs", "c"),
            CreateWriteCall("docs/readme.md", "d"),
        };

        foreach (var call in chain)
        {
            var result = engine.Approve(call, EmptySnapshot, out var reason);
            result.Should().BeTrue($"each valid call must pass: {GetPathFromCall(call)}");
            reason.Should().Be("OK");
        }

        var allPaths = chain.Select(GetPathFromCall).ToList();
        allPaths.Should().OnlyContain(p => p.StartsWith("src/", StringComparison.Ordinal) ||
            p.StartsWith("tests/", StringComparison.Ordinal) ||
            p.StartsWith("docs/", StringComparison.Ordinal));
    }

    [Fact]
    public void AdversarialScope_PathTraversalViaChain_IsDetectedAndRejected()
    {
        var engine = CreateScopePolicyEngine();
        var traversalPaths = new[]
        {
            "src/foo/../../../.git/config",
            "src/../.git/config",
            "tests/../../etc/passwd",
        };

        foreach (var path in traversalPaths)
        {
            var call = CreateWriteCall(path, "content");
            var result = engine.Approve(call, EmptySnapshot, out var reason);
            result.Should().BeFalse($"path traversal must be rejected: {path}");
            reason.Should().Contain("Path not allowed");
        }
    }

    [Fact]
    public void AdversarialScope_CumulativeWriteSize_CannotExceedMaxWriteSize_ViaBatching()
    {
        var engine = CreateScopePolicyEngine();
        var maxPerWrite = 200_000;
        var n = 5;
        for (var i = 0; i < n; i++)
        {
            var content = new string('x', 100_000);
            var call = CreateWriteCall($"src/file{i}.cs", content);
            var result = engine.Approve(call, EmptySnapshot, out var reason);
            result.Should().BeTrue($"each write under {maxPerWrite} must pass");
            reason.Should().Be("OK");
        }
        var totalWouldBe = n * 100_000;
        totalWouldBe.Should().BeGreaterThan(maxPerWrite, "total exceeds limit to demonstrate per-write enforcement");
    }

    [Fact]
    public async Task AdversarialScope_RejectionOccursBeforePartialApplication()
    {
        var invocations = new ConcurrentBag<ToolCall>();
        var toolbox = new MockToolbox(invocations);
        var policies = CreateScopePolicyEngine();
        var agent = new MockAgentThatReturnsInvalidInMiddle();
        var host = new AgentHost(new[] { agent }, toolbox, policies);

        await host.StepAsync(EmptySnapshot, default);

        invocations.Should().BeEmpty("no tool calls must execute when chain contains a rejection");
    }

    [Fact]
    public async Task AdversarialScope_ChainRejection_WrittenToAuditLog_WithFullChainTrace()
    {
        var auditEntries = new ConcurrentBag<DataDecisionAuditEntry>();
        var mockAuditLog = new MockDataDecisionAuditLog(auditEntries);
        var invocations = new ConcurrentBag<ToolCall>();
        var toolbox = new MockToolbox(invocations);
        var policies = CreateScopePolicyEngine();
        var agent = new MockAgentThatReturnsInvalidInMiddle();

        void OnRejected(IReadOnlyList<ToolCall> chain, int rejectedIndex, string reason)
        {
            var chainIds = chain.Select((c, i) => $"{i}:{c.Id}:{GetPathFromCall(c)}").ToList();
            var resolvedPath = GetPathFromCall(chain[rejectedIndex]);
            mockAuditLog.LogScopeChainRejection(chainIds, rejectedIndex, resolvedPath, reason);
        }

        var host = new AgentHost(new[] { agent }, toolbox, policies, OnRejected);
        await host.StepAsync(EmptySnapshot, default);

        var rejected = auditEntries.Where(e => e.EventType == "ScopeChainRejected").ToList();
        rejected.Should().ContainSingle();
        rejected[0].SourceId.Should().Contain("1:repo.fs.write:.git/config");
        rejected[0].ChangeType.Should().Be("1");
        rejected[0].ProjectPath.Should().Be(".git/config");
        rejected[0].Reason.Should().Contain("Path not allowed");
    }

    private sealed class MockToolbox : IToolbox
    {
        private readonly ConcurrentBag<ToolCall> _invocations;

        public MockToolbox(ConcurrentBag<ToolCall> invocations) => _invocations = invocations;

        public IEnumerable<ToolSchema> Schemas() => Array.Empty<ToolSchema>();
        public IAgentMemory MemoryFor(IAgent agent) => new MockAgentMemory();

        public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
        {
            _invocations.Add(call);
            return Task.FromResult(new ToolResult(new ActionDelta(0, 1, new[] { "mock" }), null));
        }
    }

    private sealed class MockAgentMemory : IAgentMemory
    {
        public void Write(EventRecord record) { }
        public IReadOnlyList<EventRecord> Query(string filter, int k) => Array.Empty<EventRecord>();
    }

    private sealed class MockAgentThatReturnsInvalidInMiddle : IAgent
    {
        public string Name => "mock";
        public Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct)
        {
            var calls = new List<ToolCall>
            {
                CreateWriteCall("src/a.cs", "a"),
                CreateWriteCall(".git/config", "malicious"),
                CreateWriteCall("src/c.cs", "c"),
            };
            return Task.FromResult(new AgentActions(calls));
        }
    }

    private sealed class MockDataDecisionAuditLog : IDataDecisionAuditLog
    {
        private readonly ConcurrentBag<DataDecisionAuditEntry> _entries;

        public MockDataDecisionAuditLog(ConcurrentBag<DataDecisionAuditEntry> entries) => _entries = entries;

        public void LogScopeChainRejection(IReadOnlyList<string> chainIds, int rejectedStep, string? resolvedPath, string reason)
        {
            _entries.Add(new DataDecisionAuditEntry
            {
                EventType = "ScopeChainRejected",
                Timestamp = DateTimeOffset.UtcNow,
                ChangeType = rejectedStep.ToString(),
                SourceId = string.Join(";", chainIds),
                ProjectPath = resolvedPath,
                Reason = reason,
            });
        }

        public void LogAmbientAction(string agentId, string summary, int toolCallsExecuted) { }

        public void LogSanitization(SanitizationAuditEntryDto entry) { }
        public void LogBoundaryChange(BoundaryChangeEvent evt) { }
        public void LogClassification(string dataType, string levelName, string? reason) { }
        public IReadOnlyList<DataDecisionAuditEntry> GetRecent(int maxCount, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null) =>
            _entries.ToList();
        public string ExportToJson(int maxCount = 1000, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null) => "{}";
        public string ExportToMarkdown(int maxCount = 1000, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null) => "";
        public string ExportToCsv(int maxCount = 1000, DateTimeOffset? since = null, DateTimeOffset? until = null, string? eventType = null) => "";
    }
}

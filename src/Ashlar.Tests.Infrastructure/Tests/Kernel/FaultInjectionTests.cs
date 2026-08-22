using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Rollback.Models;
using Ashlar.Core.Application.Rollback.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Rollback;
using Ashlar.Policies.Dev;
using Ashlar.Runtime;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Kernel;

/// <summary>
/// Fault injection tests for kernel safety components.
/// Validates fail-closed behavior and exception propagation under degraded conditions.
/// </summary>
[Trait("Category", "Unit")]
public sealed class FaultInjectionTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public FaultInjectionTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-fault-injection");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    private static ToolCall CreateWriteCall(string path, string content)
    {
        var json = JsonSerializer.SerializeToElement(new { path, content });
        return new ToolCall("repo.fs.write", json);
    }

    [Fact]
    public async Task ChainRejectionCallback_WhenThrows_NoToolCallsExecuted()
    {
        var invocations = new ConcurrentBag<ToolCall>();
        var toolbox = new MockToolbox(invocations);
        var policies = new PolicyEngine(new IPolicy[] { new PathAllowlist(), new MaxWriteSize(200_000) });
        var agent = new MockAgentThatReturnsInvalidInMiddle();

        void OnRejected(IReadOnlyList<ToolCall> chain, int rejectedIndex, string reason)
        {
            throw new InvalidOperationException("Audit log write failed");
        }

        var host = new AgentHost(new[] { agent }, toolbox, policies, OnRejected);

        var act = () => host.StepAsync(EmptySnapshot, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Audit log write failed");
        invocations.Should().BeEmpty("no tool calls must execute when chain is rejected");
    }

    [Fact]
    public async Task RollbackManager_WhenRestoreSnapshotThrows_PropagatesException()
    {
        var (tempDir, _) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-rollback-fault");
        var testFile = Path.Combine(tempDir, "src", "foo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(testFile)!);
        await File.WriteAllTextAsync(testFile, "original");

        var failingStore = new ThrowingOnRestoreSnapshotStore();
        var mockAuditLog = new MockAdaptationAuditLog();
        var rollbackManager = new RollbackManager(
            failingStore,
            new DependencyGraph(),
            mockAuditLog);

        rollbackManager.PrepareForInherit("adapt-1", new[] { testFile });
        await rollbackManager.BeforeInheritAsync("adapt-1");

        var act = () => rollbackManager.RollbackAsync("adapt-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("RestoreSnapshotAsync failed");
    }

    [Fact]
    public void ImmutableCoreRegistry_NullOrEmpty_ReturnsFalse()
    {
        var registry = new ImmutableCoreRegistry();

        registry.IsInImmutableCore(null!).Should().BeFalse("null must be treated as not in core (fail-closed)");
        registry.IsInImmutableCore("").Should().BeFalse("empty string must be treated as not in core (fail-closed)");
        registry.IsInImmutableCore("   ").Should().BeFalse("whitespace must be treated as not in core (fail-closed)");
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

    private sealed class ThrowingOnRestoreSnapshotStore : ISnapshotStore
    {
        private string? _snapshotId;

        public Task<string> TakeSnapshotAsync(string label, IReadOnlyList<string> componentPaths, CancellationToken ct = default)
        {
            _snapshotId = Guid.NewGuid().ToString("N");
            return Task.FromResult(_snapshotId);
        }

        public Task<IEnumerable<SnapshotEntry>> ListSnapshotsAsync(CancellationToken ct = default) =>
            Task.FromResult(Enumerable.Empty<SnapshotEntry>());

        public Task RestoreSnapshotAsync(string snapshotId, CancellationToken ct = default) =>
            throw new InvalidOperationException("RestoreSnapshotAsync failed");

        public Task DeleteSnapshotAsync(string snapshotId, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class MockAdaptationAuditLog : IAdaptationAuditLog
    {
        public Task LogAsync(AdaptationAuditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AdaptationAuditEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdaptationAuditEntry>>(Array.Empty<AdaptationAuditEntry>());
    }
}

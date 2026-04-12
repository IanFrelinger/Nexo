using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Rollback.Models;
using Nexo.Core.Application.Rollback.Ports;
using Nexo.Core.Application.SelfContext.Models;
using Nexo.Core.Application.SelfContext.Ports;
using Nexo.Core.Application.SelfImprovement.Ports;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Analysis;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.Rollback;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Safety;

/// <summary>
/// Fail-closed property tests. Every kernel safety component must default to rejection
/// when it cannot determine whether something is safe.
/// </summary>
[Trait("Category", "Safety")]
[Trait("Category", "Unit")]
public sealed class FailClosedTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public FailClosedTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-fail-closed");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    private static ToolCall CreateWriteCall(string path, string content)
    {
        var json = JsonSerializer.SerializeToElement(new { path, content });
        return new ToolCall("repo.fs.write", json);
    }

    // --- 1.1 ImmutableCoreRegistry Fail-Closed ---

    [Fact]
    public async Task ImmutableCoreRegistry_WhenStorageUnavailable_RejectsAllAdaptations()
    {
        var failures = new List<TestFailureRecord>
        {
            new()
            {
                Id = "f1",
                Timestamp = DateTimeOffset.UtcNow,
                TestName = "SomeTest",
                FilePath = "src/SomeFile.cs",
                ErrorMessage = "fail",
            },
        };

        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-failclosed-{Guid.NewGuid():N}");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<ITestFailureStore>(new MockTestFailureStore(failures))
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .AddSingleton<IImmutableCoreRegistry>(new ThrowingImmutableCoreRegistry())
            .AddSelfImprovementLoop(1)
            .BuildServiceProvider();

        var loop = services.GetRequiredService<ISelfImprovementLoop>();

        var act = () => loop.RunOnceAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*registry unavailable*");

        var report = await loop.GetLastRunReportAsync();
        report.Should().BeNull("no report when loop throws before completing");
    }

    [Fact]
    public void ImmutableCoreRegistry_WhenThrows_IsInImmutableCore_PropagatesException()
    {
        var registry = new ThrowingImmutableCoreRegistry();

        var act = () => registry.IsInImmutableCore("src/foo.cs");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*registry unavailable*");
    }

    // --- 1.2 DataDecisionAuditLog / ChainRejectionCallback Fail-Closed ---

    [Fact]
    public async Task AdaptationPipeline_WhenAuditLogWriteFails_RejectsAdaptation()
    {
        var invocations = new ConcurrentBag<ToolCall>();
        var toolbox = new CapturingToolbox(invocations);
        var policies = new PolicyEngine(new IPolicy[] { new PathAllowlist(), new MaxWriteSize(200_000) });
        var agent = new MockAgentThatReturnsInvalidPath();

        void OnRejected(IReadOnlyList<ToolCall> chain, int rejectedIndex, string reason)
        {
            throw new InvalidOperationException("Audit log write failed");
        }

        var host = new AgentHost(new[] { agent }, toolbox, policies, OnRejected);

        var act = () => host.StepAsync(EmptySnapshot, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Audit log write failed");
        invocations.Should().BeEmpty("no tool calls must execute when chain is rejected and callback throws");
    }

    [Fact]
    public async Task AdaptationPipeline_WhenAuditLogWriteFails_LeavesNoPartialState()
    {
        var invocations = new ConcurrentBag<ToolCall>();
        var toolbox = new CapturingToolbox(invocations);
        var policies = new PolicyEngine(new IPolicy[] { new PathAllowlist(), new MaxWriteSize(200_000) });
        var agent = new MockAgentThatReturnsInvalidPath();

        void OnRejected(IReadOnlyList<ToolCall> chain, int rejectedIndex, string reason)
        {
            throw new InvalidOperationException("Audit log write failed");
        }

        var host = new AgentHost(new[] { agent }, toolbox, policies, OnRejected);

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StepAsync(EmptySnapshot, default));

        invocations.Should().BeEmpty("no writes occurred");
    }

    // --- 1.3 RollbackManager Fail-Closed ---

    [Fact]
    public async Task RollbackManager_WhenSnapshotStoreFails_RejectsAdaptationBeforeWrite()
    {
        var failingSnapshotStore = new ThrowingOnTakeSnapshotStore();
        var mockAuditLog = new MockAdaptationAuditLog();
        var rollbackManager = new RollbackManager(
            failingSnapshotStore,
            new DependencyGraph(),
            mockAuditLog);

        rollbackManager.PrepareForInherit("adapt-1", new[] { "src/foo.cs" });

        var act = () => rollbackManager.BeforeInheritAsync("adapt-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TakeSnapshotAsync failed*");
    }

    [Fact]
    public async Task RollbackManager_WhenRollbackFails_PropagatesException()
    {
        var (tempDir, _) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-rollback-fail");
        var testFile = Path.Combine(tempDir, "src", "foo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(testFile)!);
        await File.WriteAllTextAsync(testFile, "original");

        var failingSnapshotStore = new FailOnRestoreSnapshotStore();
        var mockAuditLog = new MockAdaptationAuditLog();
        var rollbackManager = new RollbackManager(
            failingSnapshotStore,
            new DependencyGraph(),
            mockAuditLog);

        rollbackManager.PrepareForInherit("adapt-1", new[] { testFile });
        await rollbackManager.BeforeInheritAsync("adapt-1");

        var act = () => rollbackManager.RollbackAsync("adapt-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RestoreSnapshotAsync failed*");
    }

    // --- 1.4 AccessBoundary Fail-Closed ---

    [Fact]
    public void AccessBoundary_WhenStateUnavailable_ThrowsOnAccess()
    {
        var brokenBoundary = new ThrowingAccessBoundary();

        var act = () => _ = brokenBoundary.IsObservationPaused;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*boundary unavailable*");
    }

    [Fact]
    public async Task SelfImprovementLoop_WhenAccessBoundaryPaused_SkipsAdaptation()
    {
        var failures = new List<TestFailureRecord>
        {
            new()
            {
                Id = "f1",
                Timestamp = DateTimeOffset.UtcNow,
                TestName = "SomeTest",
                FilePath = "src/SomeFile.cs",
                ErrorMessage = "fail",
            },
        };

        var pausedBoundary = new PausedAccessBoundary();
        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-failclosed-pause-{Guid.NewGuid():N}");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<ITestFailureStore>(new MockTestFailureStore(failures))
            .AddCodeAnalyzers()
            .AddAdaptationInfrastructure(storePath)
            .AddSingleton<IAccessBoundary>(pausedBoundary)
            .AddSelfImprovementLoop(1)
            .BuildServiceProvider();

        var loop = services.GetRequiredService<ISelfImprovementLoop>();
        await loop.RunOnceAsync(CancellationToken.None);

        var report = await loop.GetLastRunReportAsync();
        (report == null || report.FixesPromoted == 0).Should().BeTrue(
            "when boundary is paused, loop skips and no adaptation should run");
    }

    // --- Test doubles ---

    private sealed class ThrowingImmutableCoreRegistry : IImmutableCoreRegistry
    {
        public IReadOnlyList<string> CoreComponentIds => throw new InvalidOperationException("registry unavailable");

        public bool IsInImmutableCore(string pathOrComponentId) =>
            throw new InvalidOperationException("registry unavailable");

        public bool IsCoreNamespace(string namespaceOrPath) =>
            throw new InvalidOperationException("registry unavailable");
    }

    private sealed class ThrowingAccessBoundary : IAccessBoundary
    {
        public bool IsObservationPaused => throw new InvalidOperationException("boundary unavailable");
        public bool IsCategoryAllowed(string category) => throw new InvalidOperationException("boundary unavailable");
        public bool IsSourceAllowed(string sourceId) => throw new InvalidOperationException("boundary unavailable");
        public bool IsSourceAllowedForProject(string sourceId, string? projectPath) => throw new InvalidOperationException("boundary unavailable");
        public void SetPause(bool paused) => throw new InvalidOperationException("boundary unavailable");
        public void SetCategoryAllowed(string category, bool allowed) => throw new InvalidOperationException("boundary unavailable");
        public void SetSourceAllowed(string sourceId, bool allowed) => throw new InvalidOperationException("boundary unavailable");
        public void SetProjectOverride(string projectPath, IReadOnlyDictionary<string, bool>? overrides) => throw new InvalidOperationException("boundary unavailable");
        public void Reset() => throw new InvalidOperationException("boundary unavailable");
        public void ApplyPolicyPack(TrustPolicyPack pack) => throw new InvalidOperationException("boundary unavailable");
        public ActiveTrustPolicyPack? GetActivePolicyPack() => throw new InvalidOperationException("boundary unavailable");
        public IReadOnlyDictionary<string, bool> GetCategoryAllowlistSnapshot() => throw new InvalidOperationException("boundary unavailable");
        public IReadOnlyDictionary<string, bool> GetSourceAllowlistSnapshot() => throw new InvalidOperationException("boundary unavailable");
#pragma warning disable CS0067
        public event Action<BoundaryChangeEvent>? BoundaryChanged;
#pragma warning restore CS0067
    }

    private sealed class PausedAccessBoundary : IAccessBoundary
    {
        public bool IsObservationPaused => true;
        public bool IsCategoryAllowed(string category) => false;
        public bool IsSourceAllowed(string sourceId) => false;
        public bool IsSourceAllowedForProject(string sourceId, string? projectPath) => false;
        public void SetPause(bool paused) { }
        public void SetCategoryAllowed(string category, bool allowed) { }
        public void SetSourceAllowed(string sourceId, bool allowed) { }
        public void SetProjectOverride(string projectPath, IReadOnlyDictionary<string, bool>? overrides) { }
        public void Reset() { }
        public void ApplyPolicyPack(TrustPolicyPack pack) { }
        public ActiveTrustPolicyPack? GetActivePolicyPack() => null;
        public IReadOnlyDictionary<string, bool> GetCategoryAllowlistSnapshot() => new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, bool> GetSourceAllowlistSnapshot() => new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
#pragma warning disable CS0067
        public event Action<BoundaryChangeEvent>? BoundaryChanged;
#pragma warning restore CS0067
    }

    private sealed class ThrowingOnTakeSnapshotStore : ISnapshotStore
    {
        public Task<string> TakeSnapshotAsync(string label, IReadOnlyList<string> componentPaths, CancellationToken ct = default) =>
            throw new InvalidOperationException("TakeSnapshotAsync failed");

        public Task<IEnumerable<SnapshotEntry>> ListSnapshotsAsync(CancellationToken ct = default) =>
            Task.FromResult(Enumerable.Empty<SnapshotEntry>());

        public Task RestoreSnapshotAsync(string snapshotId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DeleteSnapshotAsync(string snapshotId, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FailOnRestoreSnapshotStore : ISnapshotStore
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

    private sealed class MockTestFailureStore : ITestFailureStore
    {
        private readonly IReadOnlyList<TestFailureRecord> _records;

        public MockTestFailureStore(IReadOnlyList<TestFailureRecord> records) => _records = records;

        public Task RecordAsync(TestFailureRecord record, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<TestFailureRecord>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records);
    }

    private sealed class CapturingToolbox : IToolbox
    {
        private readonly ConcurrentBag<ToolCall> _invocations;

        public CapturingToolbox(ConcurrentBag<ToolCall> invocations) => _invocations = invocations;

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

    private sealed class MockAgentThatReturnsInvalidPath : IAgent
    {
        public string Name => "mock";

        public Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct)
        {
            var calls = new List<ToolCall>
            {
                CreateWriteCall("src/a.cs", "a"),
                CreateWriteCall("bin/malicious.dll", "malicious"),
                CreateWriteCall("src/c.cs", "c"),
            };
            return Task.FromResult(new AgentActions(calls));
        }
    }

    private sealed class MockAdaptationAuditLog : IAdaptationAuditLog
    {
        public Task LogAsync(AdaptationAuditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AdaptationAuditEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdaptationAuditEntry>>(Array.Empty<AdaptationAuditEntry>());
    }
}

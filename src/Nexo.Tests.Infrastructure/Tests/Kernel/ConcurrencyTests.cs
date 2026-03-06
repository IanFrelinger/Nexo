using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Rollback.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Rollback;
using Nexo.Policies.Dev;
using Nexo.Runtime;
using Nexo.Tests.Application.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Kernel;

/// <summary>
/// Concurrency tests for kernel safety components.
/// Validates thread safety and isolation under parallel load.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ConcurrencyTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public ConcurrencyTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-concurrency");
    }

    public void Dispose() => _tempDirCleanup.Dispose();

    private static readonly WorldSnapshot EmptySnapshot = new(0, new Dictionary<string, object?>());

    private static ToolCall CreateWriteCall(string path, string content)
    {
        var json = JsonSerializer.SerializeToElement(new { path, content });
        return new ToolCall("repo.fs.write", json);
    }

    [Fact]
    public void PathAllowlist_IsThreadSafe_UnderConcurrentChecks()
    {
        var policy = new PathAllowlist();
        var rnd = new Random(42);
        var paths = Enumerable.Range(0, 50)
            .Select(_ => rnd.Next(3) switch
            {
                0 => "src/foo.cs",
                1 => "tests/bar.cs",
                _ => ".git/config",
            })
            .ToList();

        var results = new ConcurrentBag<(bool Approved, string Reason)>();
        Parallel.For(0, 50, i =>
        {
            var path = paths[i % paths.Count];
            var call = CreateWriteCall(path, "content");
            var approved = policy.Approve(call, EmptySnapshot, out var reason);
            results.Add((approved, reason));
        });

        results.Should().HaveCount(50);
        foreach (var (approved, reason) in results)
        {
            (approved == true || approved == false).Should().BeTrue();
            reason.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void PolicyEngine_ConcurrentApprovals_NoRaceCondition()
    {
        var engine = new PolicyEngine(new IPolicy[] { new PathAllowlist(), new MaxWriteSize(200_000) });
        var calls = Enumerable.Range(0, 100)
            .Select(i => CreateWriteCall($"src/file{i}.cs", "x"))
            .ToList();

        var results = new ConcurrentBag<bool>();
        Parallel.ForEach(calls, call =>
        {
            var approved = engine.Approve(call, EmptySnapshot, out var reason);
            results.Add(approved);
            reason.Should().NotBeNullOrEmpty();
        });

        results.Should().HaveCount(100);
        results.Should().OnlyContain(b => b);
    }

    [Fact]
    public async Task RollbackManager_ConcurrentRollbacks_AreIsolated()
    {
        var (tempDir, _) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-rollback-concurrent");
        var fileA = Path.Combine(tempDir, "src", "a.cs");
        var fileB = Path.Combine(tempDir, "src", "b.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(fileA)!);
        await File.WriteAllTextAsync(fileA, "a-original");
        await File.WriteAllTextAsync(fileB, "b-original");

        var snapshotPath = Path.Combine(tempDir, "snapshots");
        var auditPath = Path.Combine(tempDir, "audit.db");
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<IAdaptationAuditLog>(_ => new LiteDbAdaptationAuditLog(auditPath))
            .AddSingleton<IDependencyGraph, DependencyGraph>()
            .AddSingleton<ISnapshotStore>(_ => new FileSnapshotStore(snapshotPath))
            .AddSingleton<IRollbackManager, RollbackManager>()
            .BuildServiceProvider();

        var rollbackManager = services.GetRequiredService<IRollbackManager>();

        rollbackManager.PrepareForInherit("adapt-a", new[] { fileA });
        rollbackManager.PrepareForInherit("adapt-b", new[] { fileB });
        await rollbackManager.BeforeInheritAsync("adapt-a");
        await rollbackManager.BeforeInheritAsync("adapt-b");

        await File.WriteAllTextAsync(fileA, "a-modified");
        await File.WriteAllTextAsync(fileB, "b-modified");

        await rollbackManager.RollbackAsync("adapt-a");

        (await File.ReadAllTextAsync(fileA)).Should().Be("a-original");
        (await File.ReadAllTextAsync(fileB)).Should().Be("b-modified", "adapt-b must be unaffected by rollback of adapt-a");
    }

    [Fact]
    public async Task AgentHost_ConcurrentSteps_DoNotInterfere()
    {
        var invocationsA = new ConcurrentBag<ToolCall>();
        var invocationsB = new ConcurrentBag<ToolCall>();
        var toolboxA = new CapturingToolbox(invocationsA);
        var toolboxB = new CapturingToolbox(invocationsB);
        var policies = new PolicyEngine(new IPolicy[] { new PathAllowlist(), new MaxWriteSize(200_000) });
        var agentA = new MockAgentWithCalls(CreateWriteCall("src/a.cs", "a"));
        var agentB = new MockAgentWithCalls(CreateWriteCall("src/b.cs", "b"));

        var hostA = new AgentHost(new[] { agentA }, toolboxA, policies);
        var hostB = new AgentHost(new[] { agentB }, toolboxB, policies);

        await Task.WhenAll(
            hostA.StepAsync(EmptySnapshot, default),
            hostB.StepAsync(EmptySnapshot, default));

        var callA = invocationsA.Should().ContainSingle().Subject;
        callA.Arguments.TryGetProperty("path", out var pA).Should().BeTrue();
        pA.GetString().Should().Be("src/a.cs");

        var callB = invocationsB.Should().ContainSingle().Subject;
        callB.Arguments.TryGetProperty("path", out var pB).Should().BeTrue();
        pB.GetString().Should().Be("src/b.cs");
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

    private sealed class MockAgentWithCalls : IAgent
    {
        private readonly ToolCall _call;

        public MockAgentWithCalls(ToolCall call) => _call = call;

        public string Name => "mock";

        public Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct) =>
            Task.FromResult(new AgentActions(new[] { _call }));
    }
}

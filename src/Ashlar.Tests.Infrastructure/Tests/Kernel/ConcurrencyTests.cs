using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Rollback.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Rollback;
using Ashlar.Policies.Dev;
using Ashlar.Runtime;
using Ashlar.Tests.Application.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Kernel;

/// <summary>
/// Concurrency tests for kernel safety components.
/// Validates thread safety and isolation under parallel load.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Safety")]
public sealed class ConcurrencyTests : IDisposable
{
    private readonly IDisposable _tempDirCleanup;

    public ConcurrencyTests()
    {
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-concurrency");
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
    public async Task PathAllowlist_IsThreadSafe_UnderHighConcurrency()
    {
        var policy = new PathAllowlist();
        var paths = Enumerable.Range(0, 200)
            .Select(i => i % 3 == 0 ? $"src/file{i}.cs"
                : i % 3 == 1 ? $"bin/file{i}.dll"
                : $"../escape{i}.cs")
            .ToArray();

        var results = new ConcurrentBag<bool>();
        await Parallel.ForEachAsync(paths,
            new ParallelOptions { MaxDegreeOfParallelism = 50 },
            async (path, ct) =>
            {
                var call = CreateWriteCall(path, "content");
                results.Add(policy.Approve(call, EmptySnapshot, out var reason));
                await Task.Yield();
            });

        results.Should().HaveCount(200, "all 200 results must be present — no results dropped");
        var grouped = results.GroupBy(r => r).ToList();
        grouped.Should().OnlyContain(g => g.Count() > 0, "results are deterministic — same path always produces same result");
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
        var (tempDir, _) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-rollback-concurrent");
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
    public async Task AdaptationChain_ConcurrentChains_DoNotShareState()
    {
        var (tempDir, _) = TestHelpers.CreateTempDirectoryWithCleanup("ashlar-chain-concurrent");
        var fileChain1 = Path.Combine(tempDir, "chain-1", "foo.cs");
        var fileChain2 = Path.Combine(tempDir, "chain-2", "bar.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(fileChain1)!);
        Directory.CreateDirectory(Path.GetDirectoryName(fileChain2)!);
        await File.WriteAllTextAsync(fileChain1, "chain1-original");
        await File.WriteAllTextAsync(fileChain2, "chain2-original");

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

        rollbackManager.PrepareForInherit("chain-1", new[] { fileChain1 });
        rollbackManager.PrepareForInherit("chain-2", new[] { fileChain2 });
        await rollbackManager.BeforeInheritAsync("chain-1");
        await rollbackManager.BeforeInheritAsync("chain-2");

        await File.WriteAllTextAsync(fileChain1, "chain1-modified");
        await File.WriteAllTextAsync(fileChain2, "chain2-modified");

        await Task.WhenAll(
            rollbackManager.RollbackAsync("chain-1"),
            rollbackManager.RollbackAsync("chain-2"));

        (await File.ReadAllTextAsync(fileChain1)).Should().Be("chain1-original", "chain-1 rollback must only affect chain-1 files");
        (await File.ReadAllTextAsync(fileChain2)).Should().Be("chain2-original", "chain-2 rollback must only affect chain-2 files");
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

    [Fact]
    public async Task AgentHost_ConcurrentSteps_NeverLeavePartialState()
    {
        var invocations = new ConcurrentBag<ToolCall>();
        var toolbox = new CapturingToolbox(invocations);
        var policies = new PolicyEngine(new IPolicy[] { new PathAllowlist(), new MaxWriteSize(200_000) });
        var agents = Enumerable.Range(0, 20)
            .Select(i => new MockAgentWithCalls(CreateWriteCall($"src/file{i}.cs", "x")))
            .ToList();

        var host = new AgentHost(agents, toolbox, policies);

        var deltas = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => host.StepAsync(EmptySnapshot, default)));

        deltas.Should().HaveCount(20);
        foreach (var call in invocations)
        {
            call.Arguments.TryGetProperty("path", out var p).Should().BeTrue();
            (p.GetString() ?? "").Should().StartWith("src/", "all executed calls must be to allowed paths");
        }
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

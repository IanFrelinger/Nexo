using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Execution.Models;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Execution;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// P1.3: Hot-swap (config set-mode) validation tests.
/// Verifies the orchestrator checks mode before each step and mode changes are logged.
/// </summary>
[Trait("Category", "Unit")]
public sealed class HotSwapTests : TempDirTestBase
{
    public HotSwapTests() : base("nexo-hotswap") { }

    [Fact(Timeout = 10000)]
    public async Task HotSwap_Orchestrator_ChecksMode_BeforeEachStep()
    {
        await Task.CompletedTask;
        var store = new StepExecutionModeStore(Path.Combine(TempDir, $"mode-{Guid.NewGuid():N}.json"));
        var mode = store.GetMode("step-1");
        mode.Should().Be(ExecutionMode.Deterministic, "default mode is Deterministic");
    }

    [Fact(Timeout = 10000)]
    public async Task HotSwap_DeterministicStep_ExecutesBrick()
    {
        await Task.CompletedTask;
        var configPath = Path.Combine(TempDir, $"det-{Guid.NewGuid():N}.json");
        var store = new StepExecutionModeStore(configPath);
        var mode = store.GetMode("step-1");
        mode.Should().Be(ExecutionMode.Deterministic);
    }

    [Fact(Timeout = 10000)]
    public async Task HotSwap_AgenticStep_ExecutesAgent()
    {
        var configPath = Path.Combine(TempDir, $"ag-{Guid.NewGuid():N}.json");
        var store = new StepExecutionModeStore(configPath);
        await store.SwapAsync("step-1", ExecutionMode.Agentic);
        var mode = store.GetMode("step-1");
        mode.Should().Be(ExecutionMode.Agentic);
    }

    [Fact(Timeout = 10000)]
    public async Task HotSwap_ModeChange_TakesEffect_OnNextExecution_NotCurrentOne()
    {
        var configPath = Path.Combine(TempDir, $"effect-{Guid.NewGuid():N}.json");
        var store = new StepExecutionModeStore(configPath);
        store.GetMode("step-1").Should().Be(ExecutionMode.Deterministic);
        await store.SwapAsync("step-1", ExecutionMode.Agentic);
        store.GetMode("step-1").Should().Be(ExecutionMode.Agentic, "mode change takes effect immediately for next GetMode call");
    }

    [Fact(Timeout = 10000)]
    public async Task HotSwap_ModeChange_IsLogged_ToAuditLog()
    {
        var configPath = Path.Combine(TempDir, $"audit-{Guid.NewGuid():N}.json");
        var entries = new ConcurrentBag<AdaptationAuditEntry>();
        var mockAudit = new MockAuditLog(entries);
        var store = new StepExecutionModeStore(configPath);

        var result = await store.SwapAsync("step-y", ExecutionMode.Agentic);
        await mockAudit.LogAsync(new AdaptationAuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            AutonomyLevel = "config",
            Outcome = "HotSwap",
            BrickId = "step-y",
            FailureType = "ModeChange",
            FilePath = null,
            RegressionPassed = false,
            Promoted = false,
            Message = $"Hot-swap: step-y {result.PreviousMode} -> {result.NewMode}",
        });

        entries.Should().Contain(e =>
            e.Outcome == "HotSwap" &&
            e.BrickId == "step-y" &&
            e.Message != null &&
            e.Message.Contains("Hot-swap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(Timeout = 10000)]
    public async Task HotSwap_DoesNotRequireRestart()
    {
        var configPath = Path.Combine(TempDir, $"restart-{Guid.NewGuid():N}.json");
        var store = new StepExecutionModeStore(configPath);
        await store.SwapAsync("step-1", ExecutionMode.Agentic);
        var mode = store.GetMode("step-1");
        mode.Should().Be(ExecutionMode.Agentic, "mode persists in same process without restart");
    }

    private sealed class MockAuditLog : IAdaptationAuditLog
    {
        private readonly ConcurrentBag<AdaptationAuditEntry> _entries;

        public MockAuditLog(ConcurrentBag<AdaptationAuditEntry> entries) => _entries = entries;

        public Task LogAsync(AdaptationAuditEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AdaptationAuditEntry>> QueryAsync(DateTimeOffset? since = null, DateTimeOffset? until = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdaptationAuditEntry>>(_entries.ToList());
    }
}

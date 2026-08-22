using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Execution.Models;
using Ashlar.Infrastructure.Execution;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for step execution mode store gap coverage.</summary>
public sealed class StepExecutionModeStoreGapCoverageTests
{
    [Fact]
    public void Load_parses_persisted_modes_from_json_file()
    {
        var path = Path.Combine(Path.GetTempPath(), "ashlar-modes-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, """{"step-a":"Agentic","step-b":"Deterministic"}""");

        try
        {
            var store = new StepExecutionModeStore(path, NullLogger<StepExecutionModeStore>.Instance);
            store.GetMode("step-a").Should().Be(ExecutionMode.Agentic);
            store.GetMode("step-b").Should().Be(ExecutionMode.Deterministic);
            store.GetMode("missing").Should().Be(ExecutionMode.Deterministic);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_ignores_invalid_json_without_throwing()
    {
        var path = Path.Combine(Path.GetTempPath(), "ashlar-modes-bad-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{not-json");

        try
        {
            var store = new StepExecutionModeStore(path, NullLogger<StepExecutionModeStore>.Instance);
            store.GetMode("any").Should().Be(ExecutionMode.Deterministic);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task SetModeAsync_persists_mode_to_disk()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ashlar-modes-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "modes.json");

        try
        {
            var store = new StepExecutionModeStore(path);
            await store.SetModeAsync("step-save", ExecutionMode.Agentic);

            var reloaded = new StepExecutionModeStore(path);
            reloaded.GetMode("step-save").Should().Be(ExecutionMode.Agentic);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SwapAsync_returns_previous_and_new_modes()
    {
        var path = Path.Combine(Path.GetTempPath(), "ashlar-modes-swap-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new StepExecutionModeStore(path, NullLogger<StepExecutionModeStore>.Instance);
            var result = await store.SwapAsync("step-swap", ExecutionMode.Agentic);

            result.StepId.Should().Be("step-swap");
            result.PreviousMode.Should().Be(ExecutionMode.Deterministic);
            result.NewMode.Should().Be(ExecutionMode.Agentic);
            result.Success.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

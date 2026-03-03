using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public class MetricsBackgroundAgentCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestMetricsNotFound();
            await TestMetricsSucceeds();
            return new TestResult
            {
                Name = nameof(MetricsBackgroundAgentCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All MetricsBackgroundAgentCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(MetricsBackgroundAgentCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(MetricsBackgroundAgentCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestMetricsNotFound()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("missing")).Returns((BackgroundAgentInstance?)null);
        var modeStore = new InMemoryAggressivenessModeStore();
        var logger = new Mock<ILogger<MetricsBackgroundAgentCommand>>();
        var command = new MetricsBackgroundAgentCommand(registry.Object, modeStore, logger.Object);
        var exitCode = await command.ExecuteAsync("missing", false);
        AssertEqual(1, exitCode);
    }

    private async Task TestMetricsSucceeds()
    {
        var instance = new BackgroundAgentInstance
        {
            Config = new BackgroundAgentConfig { Id = "agent1", Name = "Agent 1", Role = "test" },
            ExecutionCount = 10,
            SuccessCount = 9,
            FailureCount = 1,
            LastStartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            LastCompletedAt = DateTimeOffset.UtcNow
        };
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAgent("agent1")).Returns(instance);
        var modeStore = new InMemoryAggressivenessModeStore();
        var logger = new Mock<ILogger<MetricsBackgroundAgentCommand>>();
        var command = new MetricsBackgroundAgentCommand(registry.Object, modeStore, logger.Object);
        var exitCode = await command.ExecuteAsync("agent1", false);
        AssertEqual(0, exitCode);
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.BackgroundAgents.Logging;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public class LogsBackgroundAgentCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestLogsEmptyWhenNoStore();
            await TestLogsFromStore();
            return new TestResult { Name = nameof(LogsBackgroundAgentCommandTests), Category = "CLI", Passed = true, Message = "All LogsBackgroundAgentCommand tests passed" };
        }
        catch (AssertionException ex)
        {
            return new TestResult { Name = nameof(LogsBackgroundAgentCommandTests), Category = "CLI", Passed = false, ErrorMessage = $"Assertion failed: {ex.Message}", StackTrace = ex.StackTrace };
        }
        catch (Exception ex)
        {
            return new TestResult { Name = nameof(LogsBackgroundAgentCommandTests), Category = "CLI", Passed = false, ErrorMessage = ex.Message, StackTrace = ex.StackTrace };
        }
    }

    private async Task TestLogsEmptyWhenNoStore()
    {
        var logger = new Mock<ILogger<LogsBackgroundAgentCommand>>();
        var command = new LogsBackgroundAgentCommand(logger.Object, logStore: null);
        var exitCode = await command.ExecuteAsync("any-agent", 100, null, null, false);
        AssertEqual(0, exitCode);
    }

    private async Task TestLogsFromStore()
    {
        var store = new InMemoryAgentLogStore();
        store.Append("agent1", "Info", "Test message");
        var logger = new Mock<ILogger<LogsBackgroundAgentCommand>>();
        var command = new LogsBackgroundAgentCommand(logger.Object, store);
        var exitCode = await command.ExecuteAsync("agent1", 100, null, null, false);
        AssertEqual(0, exitCode);
    }
}

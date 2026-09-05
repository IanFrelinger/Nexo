using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.CLI.Commands.BackgroundAgent;
using Ashlar.BackgroundAgents.Logging;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for logs background agent command.</summary>
public class LogsBackgroundAgentCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test logs empty when no store.</summary>
            await TestLogsEmptyWhenNoStore();
            /// <summary>Test logs from store.</summary>
            await TestLogsFromStore();
            /// <summary>Test invalid tail limit.</summary>
            await TestLogsRejectInvalidTailWithoutReadingStore();
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
        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }

    private async Task TestLogsFromStore()
    {
        var store = new InMemoryAgentLogStore();
        store.Append("agent1", "Info", "Test message");
        var logger = new Mock<ILogger<LogsBackgroundAgentCommand>>();
        var command = new LogsBackgroundAgentCommand(logger.Object, store);
        var exitCode = await command.ExecuteAsync("agent1", 100, null, null, false);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }

    private async Task TestLogsRejectInvalidTailWithoutReadingStore()
    {
        var store = new Mock<IBackgroundAgentLogStore>(MockBehavior.Strict);
        var logger = new Mock<ILogger<LogsBackgroundAgentCommand>>();
        var command = new LogsBackgroundAgentCommand(logger.Object, store.Object);
        foreach (var tail in new[] { 0, -1 })
        {
            var exitCode = await command.ExecuteAsync("agent1", tail, null, null, true);
            AssertEqual(1, exitCode);
        }
        store.VerifyNoOtherCalls();
    }
}

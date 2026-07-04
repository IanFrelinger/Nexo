using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.BackgroundAgents.Configuration;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for mode background agent command.</summary>
public class ModeBackgroundAgentCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test get default mode.</summary>
            await TestGetDefaultMode();
            /// <summary>Test set and get mode.</summary>
            await TestSetAndGetMode();
            /// <summary>Test set invalid mode throws.</summary>
            await TestSetInvalidModeThrows();
            return new TestResult
            {
                Name = nameof(ModeBackgroundAgentCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All ModeBackgroundAgentCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ModeBackgroundAgentCommandTests),
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
                Name = nameof(ModeBackgroundAgentCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestGetDefaultMode()
    {
        var modeStore = new InMemoryAggressivenessModeStore();
        var logger = new Mock<ILogger<ModeBackgroundAgentCommand>>();
        var command = new ModeBackgroundAgentCommand(modeStore, logger.Object);
        var exitCode = await command.GetAsync(false);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }

    private async Task TestSetAndGetMode()
    {
        var modeStore = new InMemoryAggressivenessModeStore();
        var logger = new Mock<ILogger<ModeBackgroundAgentCommand>>();
        var command = new ModeBackgroundAgentCommand(modeStore, logger.Object);

        var setExitCode = await command.SetAsync("passive", false);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, setExitCode);
        AssertEqual(BackgroundAgentAggressivenessMode.Passive, modeStore.GetMode());

        await command.SetAsync("semi-active", false);
        AssertEqual(BackgroundAgentAggressivenessMode.SemiActive, modeStore.GetMode());

        await command.SetAsync("ambient", false);
        AssertEqual(BackgroundAgentAggressivenessMode.Ambient, modeStore.GetMode());
    }

    private async Task TestSetInvalidModeThrows()
    {
        var modeStore = new InMemoryAggressivenessModeStore();
        var logger = new Mock<ILogger<ModeBackgroundAgentCommand>>();
        var command = new ModeBackgroundAgentCommand(modeStore, logger.Object);

        var exitCode = await command.SetAsync("invalid-mode", false);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, exitCode);
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public class SensitivityCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestListSucceeds();
            await TestListFormatJson();
            await TestShowPublic();
            return new TestResult
            {
                TestName = nameof(SensitivityCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All SensitivityCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(SensitivityCommandTests),
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
                TestName = nameof(SensitivityCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestListSucceeds()
    {
        var registry = new DataSensitivityRegistry();
        var logger = new Mock<ILogger<SensitivityCommand>>();
        var command = new SensitivityCommand(registry, logger.Object);
        var exitCode = await command.ListAsync(false);
        AssertEqual(0, exitCode);
    }

    private async Task TestListFormatJson()
    {
        var registry = new DataSensitivityRegistry();
        var logger = new Mock<ILogger<SensitivityCommand>>();
        var command = new SensitivityCommand(registry, logger.Object);
        var exitCode = await command.ListAsync(true);
        AssertEqual(0, exitCode);
    }

    private async Task TestShowPublic()
    {
        var registry = new DataSensitivityRegistry();
        var logger = new Mock<ILogger<SensitivityCommand>>();
        var command = new SensitivityCommand(registry, logger.Object);
        var exitCode = await command.ShowAsync("Public", false);
        AssertEqual(0, exitCode);
    }
}

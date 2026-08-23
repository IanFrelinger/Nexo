using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.CLI.Commands.BackgroundAgent;
using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for sensitivity command.</summary>
public class SensitivityCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test list succeeds.</summary>
            await TestListSucceeds();
            /// <summary>Test list format json.</summary>
            await TestListFormatJson();
            /// <summary>Test show public.</summary>
            await TestShowPublic();
            return new TestResult
            {
                Name = nameof(SensitivityCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All SensitivityCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(SensitivityCommandTests),
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
                Name = nameof(SensitivityCommandTests),
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
        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }

    private async Task TestListFormatJson()
    {
        var registry = new DataSensitivityRegistry();
        var logger = new Mock<ILogger<SensitivityCommand>>();
        var command = new SensitivityCommand(registry, logger.Object);
        var exitCode = await command.ListAsync(true);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }

    private async Task TestShowPublic()
    {
        var registry = new DataSensitivityRegistry();
        var logger = new Mock<ILogger<SensitivityCommand>>();
        var command = new SensitivityCommand(registry, logger.Object);
        var exitCode = await command.ShowAsync("Public", false);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, exitCode);
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI;
using Nexo.CLI.Commands;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Configuration.Models;
using Nexo.Core.Application.Configuration.UseCases.GetConfiguration;
using Nexo.Core.Application.Testing.Abstractions;
using TestingTestResult = Nexo.Core.Application.Testing.Models.TestResult;

namespace Nexo.Tests.CLI.Tests.Commands;

public class ConfigCommandTests : UnitTestBase
{
    public override async Task<TestingTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSuccessfulConfigLoad();
            await TestJsonOutput();
            await TestVerboseOutput();
            await TestGeneralException();

            return new TestingTestResult
            {
                TestName = nameof(ConfigCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All ConfigCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(ConfigCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(ConfigCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulConfigLoad()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ConfigCommand>>();

        var config = new NexoConfiguration
        {
            Analysis = new AnalysisConfiguration
            {
                EnabledRules = new[] { "SecurityScan", "CodeQuality" },
                MaxComplexityThreshold = 20,
                EnableSecurityScan = true,
                EnableCodeQuality = true
            },
            Validation = new ValidationConfiguration
            {
                TimeoutSeconds = 300,
                FailOnNoTests = false,
                TestProjectPatterns = new[] { "*Test*.csproj" }
            },
            Logging = new LoggingConfiguration
            {
                Level = "Information",
                EnableStructuredLogging = true,
                EnableProgressIndicators = true
            }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<GetConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var command = new ConfigCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        // ConfigCommand writes directly to Console.Out, so we can't easily verify the output
        // But we can verify it doesn't throw and returns Ok
    }

    private async Task TestJsonOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ConfigCommand>>();

        var config = new NexoConfiguration
        {
            Analysis = new AnalysisConfiguration(),
            Validation = new ValidationConfiguration(),
            Logging = new LoggingConfiguration()
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<GetConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var command = new ConfigCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(true, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        // ConfigCommand writes JSON directly to Console.Out in JSON mode
    }

    private async Task TestVerboseOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ConfigCommand>>();

        var config = new NexoConfiguration
        {
            Analysis = new AnalysisConfiguration(),
            Validation = new ValidationConfiguration(),
            Logging = new LoggingConfiguration()
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<GetConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var command = new ConfigCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(false, true);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderProgressStart(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderProgressComplete(It.IsAny<string>()), Times.Once);
    }

    private async Task TestGeneralException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ConfigCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<GetConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var command = new ConfigCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(false, false);

        AssertEqual((int)ExitCode.UnexpectedError, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.IsAny<string>()), Times.Once);
    }
}


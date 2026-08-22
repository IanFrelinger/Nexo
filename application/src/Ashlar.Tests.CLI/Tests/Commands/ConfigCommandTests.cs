using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.CLI;
using Ashlar.CLI.Commands;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Configuration.Models;
using Ashlar.Core.Application.Configuration.UseCases.GetConfiguration;
using Ashlar.Core.Application.Testing.Abstractions;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for config command.</summary>
public class ConfigCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful config load.</summary>
            await TestSuccessfulConfigLoad();
            /// <summary>Test json output.</summary>
            await TestJsonOutput();
            /// <summary>Test verbose output.</summary>
            await TestVerboseOutput();
            /// <summary>Test general exception.</summary>
            await TestGeneralException();

            return new TestResult
            {
                Name = nameof(ConfigCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All ConfigCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ConfigCommandTests),
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
                Name = nameof(ConfigCommandTests),
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

        var config = new AshlarConfiguration
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

        var config = new AshlarConfiguration
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

        var config = new AshlarConfiguration
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


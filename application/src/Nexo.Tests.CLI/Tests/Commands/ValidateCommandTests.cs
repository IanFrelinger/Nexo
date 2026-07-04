using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI;
using Nexo.CLI.Commands;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for validate command.</summary>
public class ValidateCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful validation.</summary>
            await TestSuccessfulValidation();
            /// <summary>Test failed validation.</summary>
            await TestFailedValidation();
            /// <summary>Test json output.</summary>
            await TestJsonOutput();
            /// <summary>Test verbose output.</summary>
            await TestVerboseOutput();
            /// <summary>Test validation exception.</summary>
            await TestValidationException();
            /// <summary>Test general exception.</summary>
            await TestGeneralException();

            return new TestResult
            {
                Name = nameof(ValidateCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All ValidateCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ValidateCommandTests),
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
                Name = nameof(ValidateCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulValidation()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ValidateCommand>>();

        var result = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 5,
            TestsPassed = 5,
            TestsFailed = 0
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunValidationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ValidateCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderValidationResult(result, false), Times.Once);
    }

    private async Task TestFailedValidation()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ValidateCommand>>();

        var result = new ValidationResult
        {
            Passed = false,
            Message = "Some tests failed",
            TestsRun = 5,
            TestsPassed = 3,
            TestsFailed = 2
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunValidationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ValidateCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("test-filter", false, false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        mockRenderer.Verify(r => r.RenderValidationResult(result, false), Times.Once);
    }

    private async Task TestJsonOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ValidateCommand>>();

        var result = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 3,
            TestsPassed = 3,
            TestsFailed = 0
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunValidationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ValidateCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, true, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderValidationResult(result, true), Times.Once);
    }

    private async Task TestVerboseOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ValidateCommand>>();

        var result = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 3,
            TestsPassed = 3,
            TestsFailed = 0
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunValidationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new ValidateCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, false, true);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderProgressStart(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderProgressComplete(It.IsAny<string>()), Times.Once);
    }

    private async Task TestValidationException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ValidateCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunValidationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation failed", ErrorCodes.ValidationTestExecutionFailed));

        var command = new ValidateCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, false, false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        mockRenderer.Verify(r => r.RenderErrorWithCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    private async Task TestGeneralException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ValidateCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunValidationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var command = new ValidateCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, false, false);

        AssertEqual((int)ExitCode.UnexpectedError, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.IsAny<string>()), Times.Once);
    }
}


using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI;
using Nexo.CLI.Commands;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Domain.Exceptions;
using Nexo.Core.Domain.Values;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for analyze command.</summary>
public class AnalyzeCommandTests : UnitTestBase
{
    private DirectoryInfo? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _tempDir = TestHelpers.CreateTempDirectory();
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempDir != null)
        {
            TestHelpers.CleanupTempDirectory(_tempDir);
        }
        return Task.CompletedTask;
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful analysis no violations.</summary>
            await TestSuccessfulAnalysisNoViolations();
            /// <summary>Test successful analysis with violations.</summary>
            await TestSuccessfulAnalysisWithViolations();
            /// <summary>Test json output.</summary>
            await TestJsonOutput();
            /// <summary>Test verbose output.</summary>
            await TestVerboseOutput();
            /// <summary>Test analysis exception.</summary>
            await TestAnalysisException();
            /// <summary>Test unauthorized access exception.</summary>
            await TestUnauthorizedAccessException();
            /// <summary>Test general exception.</summary>
            await TestGeneralException();

            return new TestResult
            {
                Name = nameof(AnalyzeCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All AnalyzeCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(AnalyzeCommandTests),
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
                Name = nameof(AnalyzeCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulAnalysisNoViolations()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AnalyzeCommand>>();

        var result = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<AnalyzeCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AnalyzeCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(_tempDir!, false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderAnalysisResult(result, false), Times.Once);
    }

    private async Task TestSuccessfulAnalysisWithViolations()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AnalyzeCommand>>();

        var result = new AnalysisResult
        {
            HasViolations = true,
            Violations = new List<Violation>
            {
                new Violation { Rule = "TestRule", Message = "Test violation", FilePath = "test.cs", Severity = RiskLevel.High }
            },
            TotalViolations = 1
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<AnalyzeCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AnalyzeCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(_tempDir!, false, false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        mockRenderer.Verify(r => r.RenderAnalysisResult(result, false), Times.Once);
    }

    private async Task TestJsonOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AnalyzeCommand>>();

        var result = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<AnalyzeCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AnalyzeCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(_tempDir!, true, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderAnalysisResult(result, true), Times.Once);
    }

    private async Task TestVerboseOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AnalyzeCommand>>();

        var result = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<AnalyzeCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AnalyzeCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(_tempDir!, false, true);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderProgressStart(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderProgressComplete(It.IsAny<string>()), Times.Once);
    }

    private async Task TestAnalysisException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AnalyzeCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<AnalyzeCodeCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AnalysisException("Analysis failed", ErrorCodes.AnalysisRuleFailure));

        var command = new AnalyzeCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(_tempDir!, false, false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        mockRenderer.Verify(r => r.RenderErrorWithCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    private async Task TestUnauthorizedAccessException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AnalyzeCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<AnalyzeCodeCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        var command = new AnalyzeCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(_tempDir!, false, false);

        AssertEqual((int)ExitCode.PolicyViolation, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.Is<string>(s => s.Contains("Policy violation"))), Times.Once);
    }

    private async Task TestGeneralException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AnalyzeCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<AnalyzeCodeCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var command = new AnalyzeCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(_tempDir!, false, false);

        AssertEqual((int)ExitCode.UnexpectedError, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.IsAny<string>()), Times.Once);
    }
}


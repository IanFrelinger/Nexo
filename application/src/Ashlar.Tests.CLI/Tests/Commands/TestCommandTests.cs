using System.IO;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.CLI;
using Ashlar.CLI.Commands;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Application.Testing.UseCases.RunTests;
using Ashlar.Core.Application.Testing.Abstractions;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for test command.</summary>
public class TestCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful test execution.</summary>
            await TestSuccessfulTestExecution();
            /// <summary>Test failed test execution.</summary>
            await TestFailedTestExecution();
            /// <summary>Test json output.</summary>
            await TestJsonOutput();
            /// <summary>Test verbose output.</summary>
            await TestVerboseOutput();
            /// <summary>Test with filter.</summary>
            await TestWithFilter();
            /// <summary>Test general exception.</summary>
            await TestGeneralException();
            /// <summary>Test json verbose stdout stays parseable.</summary>
            await TestJsonVerboseStdoutStaysParseable();

            return new TestResult
            {
                Name = nameof(TestCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All TestCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(TestCommandTests),
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
                Name = nameof(TestCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulTestExecution()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<TestCommand>>();

        var result = new TestExecutionResult
        {
            TotalTests = 5,
            PassedTests = 5,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(100),
            Results = new List<TestResult>
            {
                new TestResult { Name = "Test1", Category = "Unit", Passed = true },
                new TestResult { Name = "Test2", Category = "Unit", Passed = true }
            },
            Categories = new List<string> { "Unit" }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new TestCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        // TestCommand writes directly to Console.Out, so we can't easily verify the output
    }

    private async Task TestFailedTestExecution()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<TestCommand>>();

        var result = new TestExecutionResult
        {
            TotalTests = 5,
            PassedTests = 3,
            FailedTests = 2,
            TotalDuration = TimeSpan.FromMilliseconds(100),
            Results = new List<TestResult>
            {
                new TestResult { Name = "Test1", Category = "Unit", Passed = true },
                new TestResult { Name = "Test2", Category = "Unit", Passed = false, ErrorMessage = "Test failed" }
            },
            Categories = new List<string> { "Unit" }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Redirect console output to prevent it from being captured as test output
        try
        {
            var stringWriter = new StringWriter();  // not disposed on purpose: a disposed writer left in Console.Out poisons later tests
            Console.SetOut(stringWriter);
            Console.SetError(stringWriter);

            var command = new TestCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
            var exitCode = await command.ExecuteAsync(null, false, false);

            AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    private async Task TestJsonOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<TestCommand>>();

        var result = new TestExecutionResult
        {
            TotalTests = 3,
            PassedTests = 3,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(50),
            Results = new List<TestResult>(),
            Categories = new List<string>()
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new TestCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, true, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        // TestCommand writes JSON directly to Console.Out in JSON mode
    }

    private async Task TestVerboseOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<TestCommand>>();

        var result = new TestExecutionResult
        {
            TotalTests = 3,
            PassedTests = 3,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(50),
            Results = new List<TestResult>(),
            Categories = new List<string>()
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new TestCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, false, true);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderProgressStart(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderProgressComplete(It.IsAny<string>()), Times.Once);
    }

    private async Task TestWithFilter()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<TestCommand>>();

        var result = new TestExecutionResult
        {
            TotalTests = 1,
            PassedTests = 1,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(10),
            Results = new List<TestResult>
            {
                new TestResult { Name = "FilteredTest", Category = "Unit", Passed = true }
            },
            Categories = new List<string> { "Unit" }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new TestCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("Filtered", false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockMediator.Verify(m => m.Send(It.Is<RunTestsCommand>(c => c.Filter == "Filtered"), It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestGeneralException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<TestCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var command = new TestCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(null, false, false);

        AssertEqual((int)ExitCode.UnexpectedError, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.IsAny<string>()), Times.Once);
    }

    private async Task TestJsonVerboseStdoutStaysParseable()
    {
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<TestCommand>>();

        var result = new TestExecutionResult
        {
            TotalTests = 2,
            PassedTests = 2,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(20),
            Results = new List<TestResult>(),
            Categories = new List<string>()
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // The real renderer, not the Moq one the other cases use: which stream the progress markers
        // land on is the thing under test, and a mock renderer writes to neither. The six mock-based
        // suites prove the call HAPPENED and never where it went, which is how this survived.
        // Not disposed on purpose: a disposed writer left in Console.Out poisons later tests.
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        int exitCode;
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var command = new TestCommand(mockMediator.Object, new ConsoleRenderer(), mockLogger.Object);
            exitCode = await command.ExecuteAsync(null, true, true);
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }

        AssertEqual((int)ExitCode.Ok, exitCode);

        // --format-json --verbose has to leave stdout holding the document and nothing else.
        var captured = stdout.ToString().Trim();
        try
        {
            JsonDocument.Parse(captured).Dispose();
        }
        catch (JsonException ex)
        {
            throw new AssertionException(
                $"stdout under --format-json --verbose did not parse as JSON ({ex.Message}): {captured}");
        }

        var diagnostics = stderr.ToString();
        AssertTrue(diagnostics.Contains("[progress]"), "progress start missing from standard error");
        AssertTrue(diagnostics.Contains("[complete]"), "progress completion missing from standard error");
    }
}


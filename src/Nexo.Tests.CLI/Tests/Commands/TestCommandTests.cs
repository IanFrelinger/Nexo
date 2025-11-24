using System.IO;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI;
using Nexo.CLI.Commands;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Testing.Abstractions;
using TestingTestResult = Nexo.Core.Application.Testing.Models.TestResult;

namespace Nexo.Tests.CLI.Tests.Commands;

public class TestCommandTests : UnitTestBase
{
    public override async Task<TestingTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSuccessfulTestExecution();
            await TestFailedTestExecution();
            await TestJsonOutput();
            await TestVerboseOutput();
            await TestWithFilter();
            await TestGeneralException();

            return new TestingTestResult
            {
                TestName = nameof(TestCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All TestCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(TestCommandTests),
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
                TestName = nameof(TestCommandTests),
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
                new TestResult { TestName = "Test1", Category = "Unit", Passed = true },
                new TestResult { TestName = "Test2", Category = "Unit", Passed = true }
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
                new TestResult { TestName = "Test1", Category = "Unit", Passed = true },
                new TestResult { TestName = "Test2", Category = "Unit", Passed = false, ErrorMessage = "Test failed" }
            },
            Categories = new List<string> { "Unit" }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunTestsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Redirect console output to prevent it from being captured as test output
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            Console.SetError(stringWriter);

            var command = new TestCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
            var exitCode = await command.ExecuteAsync(null, false, false);

            AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
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
                new TestResult { TestName = "FilteredTest", Category = "Unit", Passed = true }
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
}


using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Core.Application.Testing.UseCases.RunTests;

namespace Nexo.Core.Application.Testing.UseCases.RunTests;

/// <summary>
/// MediatR handler for running tests.
/// 
/// Responsibilities:
/// - Executes test suites via ITestRunner
/// - Supports optional test filtering
/// - Tracks test execution progress
/// - Logs test results and metrics
/// 
/// Part of the Application layer's use case pattern, following CQRS principles.
/// </summary>
public class RunTestsHandler : IRequestHandler<RunTestsCommand, TestExecutionResult>
{
    private readonly ITestRunner _testRunner;
    private readonly ILogger<RunTestsHandler> _logger;

    public RunTestsHandler(
        ITestRunner testRunner,
        ILogger<RunTestsHandler> logger)
    {
        _testRunner = testRunner ?? throw new ArgumentNullException(nameof(testRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TestExecutionResult> Handle(
        RunTestsCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running tests with filter: {Filter}", request.Filter ?? "none");

        var result = await _testRunner.RunTestsAsync(
            request.Filter,
            request.Progress,
            cancellationToken);

        _logger.LogInformation(
            "Tests completed: {Passed}/{Total} passed in {Duration}ms",
            result.PassedTests,
            result.TotalTests,
            result.TotalDuration.TotalMilliseconds);

        return result;
    }
}


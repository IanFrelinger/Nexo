using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Core.Application.Testing.UseCases.RunTests;

namespace Nexo.Core.Application.Testing.UseCases.RunTests;

/// <summary>
/// Handler for running tests.
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


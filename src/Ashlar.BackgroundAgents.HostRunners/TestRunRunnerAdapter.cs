using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.Testing;
using Ashlar.Core.Application.Testing.UseCases.RunTests;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// Dog-fooded implementation of ITestRunRunner: runs the application's own test pipeline
/// (RunTestsCommand / ITestRunner) so tester background agents can run the framework's tests.
/// </summary>
public sealed class TestRunRunnerAdapter : ITestRunRunner
{
    private readonly IMediator _mediator;
    private readonly ILogger<TestRunRunnerAdapter> _logger;

    public TestRunRunnerAdapter(
        IMediator mediator,
        ILogger<TestRunRunnerAdapter> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TestRunResult> RunAsync(string? filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(new RunTestsCommand(filter, null), cancellationToken).ConfigureAwait(false);
            var success = result.FailedTests == 0;
            var summary = result.TotalTests == 0
                ? "No tests run."
                : $"{result.PassedTests}/{result.TotalTests} passed, {result.FailedTests} failed.";
            _logger.LogDebug("Test run (filter: {Filter}): {Summary}", filter ?? "(all)", summary);
            return new TestRunResult(success, result.TotalTests, result.PassedTests, result.FailedTests, summary);
        }
        catch (Exception ex)
        {
            return new TestRunResult(false, 0, 0, 0, BackgroundAgentAdapterFailure.LogAndMessage(_logger, ex, $"Test run failed: {ex.Message}"));
        }
    }
}

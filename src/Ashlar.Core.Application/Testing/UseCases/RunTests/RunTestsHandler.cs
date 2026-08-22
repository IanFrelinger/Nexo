using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Core.Application.Maintenance.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Application.Testing.Ports;
using Ashlar.Core.Application.Testing.UseCases.RunTests;

namespace Ashlar.Core.Application.Testing.UseCases.RunTests;

/// <summary>
/// MediatR handler for running tests.
///
/// Responsibilities:
/// - Executes test suites via ITestRunner
/// - Supports optional test filtering
/// - Tracks test execution progress
/// - Logs test results and metrics
/// - Optionally runs test-artifacts cleanup before/after (when ASHLAR_CLEAN_BEFORE_TEST / ASHLAR_CLEAN_AFTER_TEST=1)
///
/// Part of the Application layer's use case pattern, following CQRS principles.
/// </summary>
public class RunTestsHandler : IRequestHandler<RunTestsCommand, TestExecutionResult>
{
    private readonly ITestRunner _testRunner;
    private readonly IArtifactCleanupService? _cleanupService;
    private readonly ArtifactCleanupOptions _cleanupOptions;
    private readonly ILogger<RunTestsHandler> _logger;

    /// <summary>Creates a handler that runs tests via <see cref="ITestRunner"/>.</summary>
    /// <param name="testRunner">Service that executes test suites.</param>
    /// <param name="logger">Logger for test run progress and results.</param>
    /// <param name="cleanupService">Optional artifact cleanup service.</param>
    /// <param name="cleanupOptions">Optional cleanup configuration.</param>
    public RunTestsHandler(
        ITestRunner testRunner,
        ILogger<RunTestsHandler> logger,
        IArtifactCleanupService? cleanupService = null,
        IOptions<ArtifactCleanupOptions>? cleanupOptions = null)
    {
        _testRunner = testRunner ?? throw new ArgumentNullException(nameof(testRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cleanupService = cleanupService;
        _cleanupOptions = cleanupOptions?.Value ?? new ArtifactCleanupOptions();
    }

    /// <summary>Handles the command by running tests with optional filtering and cleanup.</summary>
    /// <param name="request">Command containing optional filter and progress callback.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Aggregated test execution results.</returns>
    public async Task<TestExecutionResult> Handle(
        RunTestsCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running tests with filter: {Filter}", request.Filter ?? "none");

        var repoRoot = _cleanupOptions.RepoRoot ?? RepoPathResolver.FindRepoRoot();

        if (_cleanupService != null && _cleanupOptions.CleanBeforeTest)
        {
            try
            {
                var beforeResult = await _cleanupService.CleanAsync("test-artifacts", new ArtifactCleanupContext(RepoRoot: repoRoot), cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Pre-test cleanup: {Bytes} bytes reclaimed", beforeResult.BytesReclaimed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pre-test cleanup failed; continuing with test run");
            }
        }

        var result = await _testRunner.RunTestsAsync(
            request.Filter,
            request.Progress,
            cancellationToken);

        if (_cleanupService != null && _cleanupOptions.CleanAfterTest)
        {
            try
            {
                var afterResult = await _cleanupService.CleanAsync("test-artifacts", new ArtifactCleanupContext(RepoRoot: repoRoot), cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Post-test cleanup: {Bytes} bytes reclaimed", afterResult.BytesReclaimed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Post-test cleanup failed");
            }
        }

        _logger.LogInformation(
            "Tests completed: {Passed}/{Total} passed in {Duration}ms",
            result.PassedTests,
            result.TotalTests,
            result.TotalDuration.TotalMilliseconds);

        return result;
    }
}


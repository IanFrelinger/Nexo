using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Core.Application.Common.Models;

namespace Nexo.Infrastructure.Testing;

/// <summary>
/// Adapter that integrates Unity testing into the ITestRunner infrastructure.
/// 
/// Wraps UnityInfrastructureTestRunner to provide Unity test execution via ITestRunner interface.
/// Allows Unity tests to be executed through the standard test runner infrastructure.
/// 
/// Part of the Infrastructure layer, implementing ITestRunner for Unity environment.
/// </summary>
public class UnityTestRunnerAdapter : ITestRunner
{
    private readonly UnityInfrastructureTestRunner _unityRunner;
    private readonly ILogger<UnityTestRunnerAdapter> _logger;

    public UnityTestRunnerAdapter(
        ILogger<UnityTestRunnerAdapter> logger,
        ILoggerFactory loggerFactory,
        string projectRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unityRunner = new UnityInfrastructureTestRunner(
            loggerFactory.CreateLogger<UnityInfrastructureTestRunner>(),
            projectRoot);
    }

    public async Task<TestExecutionResult> RunTestsAsync(
        string? filter = null,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running Unity tests with filter: {Filter}", filter ?? "none");

        // Unity test runner executes infrastructure tests
        // Filter can be used to select specific test phases if needed
        var result = await _unityRunner.RunInfrastructureTestsAsync(progress, cancellationToken);

        _logger.LogInformation(
            "Unity tests completed: {Passed}/{Total} passed in {Duration}ms",
            result.PassedTests,
            result.TotalTests,
            result.TotalDuration.TotalMilliseconds);

        return result;
    }
}

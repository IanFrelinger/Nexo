using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Core.Application.Validation.UseCases.RunValidation;

/// <summary>
/// MediatR handler for running validation tests.
/// 
/// Responsibilities:
/// - Executes validation tests via IValidationService
/// - Records execution metrics (duration, test counts, pass/fail rates)
/// - Handles errors and exceptions
/// - Logs validation progress and results
/// 
/// Part of the Application layer's use case pattern, following CQRS principles.
/// </summary>
public class RunValidationHandler : IRequestHandler<RunValidationCommand, ValidationResult>
{
    private readonly IValidationService _validationService;
    private readonly ILogger<RunValidationHandler> _logger;
    private readonly IMetricsCollector? _metricsCollector;

    public RunValidationHandler(
        IValidationService validationService,
        ILogger<RunValidationHandler> logger,
        IMetricsCollector? metricsCollector = null)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector;
    }

    /// <summary>
    /// Handles the RunValidationCommand by running validation tests.
    /// </summary>
    /// <param name="request">Command containing optional test filter and progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with test counts and pass/fail status</returns>
    public async Task<ValidationResult> Handle(
        RunValidationCommand request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Starting validation with filter: {Filter}",
            request.Filter ?? "none");

        try
        {
            var result = await _validationService.ValidateAsync(
                request.Filter,
                request.Progress,
                cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _metricsCollector?.RecordExecutionTime("Validation", duration);
            _metricsCollector?.IncrementCounter("Validation.Executed");
            _metricsCollector?.IncrementCounter("Validation.TestsRun", result.TestsRun);
            _metricsCollector?.IncrementCounter("Validation.TestsPassed", result.TestsPassed);
            _metricsCollector?.IncrementCounter("Validation.TestsFailed", result.TestsFailed);

            _logger.LogInformation(
                "Validation completed. Passed: {Passed}, Tests: {PassedCount}/{TotalCount} in {Duration}ms",
                result.Passed,
                result.TestsPassed,
                result.TestsRun,
                duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _metricsCollector?.IncrementCounter("Validation.Errors");
            _logger.LogError(ex, "Unexpected error during validation");
            throw new ValidationException(
                $"Validation failed with filter: {request.Filter ?? "none"}",
                ex);
        }
    }
}


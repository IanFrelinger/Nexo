using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Core.Application.Validation.UseCases.RunValidation;

/// <summary>
/// Handler for running validation tests.
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


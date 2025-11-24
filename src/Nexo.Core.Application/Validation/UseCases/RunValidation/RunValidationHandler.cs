using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Core.Application.Validation.UseCases.RunValidation;

/// <summary>
/// Handler for running validation tests.
/// </summary>
public class RunValidationHandler : IRequestHandler<RunValidationCommand, ValidationResult>
{
    private readonly IValidationService _validationService;
    private readonly ILogger<RunValidationHandler> _logger;

    public RunValidationHandler(
        IValidationService validationService,
        ILogger<RunValidationHandler> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationResult> Handle(
        RunValidationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting validation with filter: {Filter}",
            request.Filter ?? "none");

        try
        {
            var result = await _validationService.ValidateAsync(
                request.Filter,
                cancellationToken);

            _logger.LogInformation(
                "Validation completed. Passed: {Passed}, Tests: {PassedCount}/{TotalCount}",
                result.Passed,
                result.TestsPassed,
                result.TestsRun);

            return result;
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Unexpected error during validation");
            throw new ValidationException(
                $"Validation failed with filter: {request.Filter ?? "none"}",
                ex);
        }
    }
}


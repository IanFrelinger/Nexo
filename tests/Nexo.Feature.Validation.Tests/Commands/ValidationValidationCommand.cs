using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Validation.Tests.Commands;

/// <summary>
/// Command for validating Validation functionality with proper logging and timeouts.
/// </summary>
public class ValidationValidationCommand
{
    private readonly ILogger<ValidationValidationCommand> _logger;

    public ValidationValidationCommand(ILogger<ValidationValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Validation model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateValidationModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Validation model validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var validationError = new ValidationError(
                field: "projectName",
                message: "Project name is required",
                severity: Nexo.Shared.Enums.ValidationSeverity.Error,
                errorCode: "REQUIRED_FIELD"
            );
            var validationWarning = new ValidationWarning(
                code: "RECOMMENDED_FIELD",
                message: "Description is recommended",
                line: 1,
                column: 1
            );
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Validation model validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = validationError.Field == "projectName" &&
                        validationWarning.Code == "RECOMMENDED_FIELD" &&
                        validationError.Severity == Nexo.Shared.Enums.ValidationSeverity.Error &&
                        validationError.ErrorCode == "REQUIRED_FIELD" &&
                        validationWarning.Message == "Description is recommended";
            _logger.LogInformation("Validation model validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Validation model validation");
            return false;
        }
    }
} 
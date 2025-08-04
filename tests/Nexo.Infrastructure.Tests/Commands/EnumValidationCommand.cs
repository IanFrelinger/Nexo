using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Enums;
using System;

namespace Nexo.Infrastructure.Tests.Commands;

/// <summary>
/// Command for validating enum values in the Infrastructure layer.
/// </summary>
public class EnumValidationCommand
{
    private readonly ILogger<EnumValidationCommand> _logger;

    public EnumValidationCommand(ILogger<EnumValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that all CommandOutputType enum values are properly defined.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateCommandOutputTypeEnum(int timeoutMs = 5000)
    {
        _logger.LogInformation("Starting CommandOutputType enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Test that enum values are accessible
            var standardOutput = Enum.IsDefined(typeof(CommandOutputType), CommandOutputType.StandardOutput);
            var standardError = Enum.IsDefined(typeof(CommandOutputType), CommandOutputType.StandardError);
            var info = Enum.IsDefined(typeof(CommandOutputType), CommandOutputType.Info);
            var progress = Enum.IsDefined(typeof(CommandOutputType), CommandOutputType.Progress);
            var completed = Enum.IsDefined(typeof(CommandOutputType), CommandOutputType.Completed);
            var failed = Enum.IsDefined(typeof(CommandOutputType), CommandOutputType.Failed);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Enum validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = standardOutput && standardError && info && progress && completed && failed;
            _logger.LogInformation("CommandOutputType enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CommandOutputType enum validation");
            return false;
        }
    }
} 
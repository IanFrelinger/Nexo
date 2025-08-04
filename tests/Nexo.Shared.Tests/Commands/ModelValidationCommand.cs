using Microsoft.Extensions.Logging;
using Nexo.Shared.Models;
using System;
using System.Collections.Generic;

namespace Nexo.Shared.Tests.Commands;

/// <summary>
/// Command for validating Shared models with proper logging and timeouts.
/// </summary>
public class ModelValidationCommand
{
    private readonly ILogger<ModelValidationCommand> _logger;

    public ModelValidationCommand(ILogger<ModelValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates BuildConfiguration model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateBuildConfiguration(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting BuildConfiguration validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var config = new BuildConfiguration
            {
                Configuration = "Release",
                TargetFramework = "net8.0",
                Platform = "x64",
                Clean = true,
                Restore = false,
                RestorePackages = false,
                RunCodeAnalysis = true,
                TreatWarningsAsErrors = false
            };

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("BuildConfiguration validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = config.Configuration == "Release" && 
                        config.TargetFramework == "net8.0" && 
                        config.Platform == "x64" &&
                        config.Clean &&
                        !config.Restore &&
                        !config.RestorePackages &&
                        config.RunCodeAnalysis &&
                        !config.TreatWarningsAsErrors;
            
            _logger.LogInformation("BuildConfiguration validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BuildConfiguration validation");
            return false;
        }
    }

    /// <summary>
    /// Validates BuildResult model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateBuildResult(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting BuildResult validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var result = new BuildResult
            {
                IsSuccess = true,
                Output = "Build completed successfully",
                Errors = "",
                ExecutionTimeMs = 30000,
                OutputFiles = new List<string> { "app.dll", "app.pdb" }
            };

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("BuildResult validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var validationResult = result.IsSuccess && 
                                 result.Output == "Build completed successfully" &&
                                 result.Errors == "" &&
                                 result.ExecutionTimeMs == 30000 &&
                                 result.OutputFiles.Count == 2 &&
                                 result.OutputFiles[0] == "app.dll" &&
                                 result.OutputFiles[1] == "app.pdb";
            
            _logger.LogInformation("BuildResult validation completed: {Result}", validationResult);
            
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BuildResult validation");
            return false;
        }
    }

    /// <summary>
    /// Validates CommandResult model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateCommandResult(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting CommandResult validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var result = new CommandResult
            {
                IsSuccess = true,
                ExitCode = 0,
                Output = "Command executed successfully",
                Error = "",
                ExecutionTimeMs = 1500
            };

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("CommandResult validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var validationResult = result.IsSuccess && 
                                 result.ExitCode == 0 && 
                                 result.Output == "Command executed successfully" &&
                                 result.Error == "" &&
                                 result.ExecutionTimeMs == 1500;
            
            _logger.LogInformation("CommandResult validation completed: {Result}", validationResult);
            
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CommandResult validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ValidationResult model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateValidationResult(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ValidationResult validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var result = ValidationResult.Failure("Required field missing");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ValidationResult validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var validationResult = !result.IsValid && 
                                 result.Errors.Count == 1 &&
                                 result.Errors[0].Message == "Required field missing";
            
            _logger.LogInformation("ValidationResult validation completed: {Result}", validationResult);
            
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ValidationResult validation");
            return false;
        }
    }
} 
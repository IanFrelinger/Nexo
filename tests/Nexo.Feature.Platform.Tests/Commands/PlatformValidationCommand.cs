using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Models;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Platform.Tests.Commands;

/// <summary>
/// Command for validating Platform functionality with proper logging and timeouts.
/// </summary>
public class PlatformValidationCommand
{
    private readonly ILogger<PlatformValidationCommand> _logger;

    public PlatformValidationCommand(ILogger<PlatformValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Platform interface definitions.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidatePlatformInterfaces(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Platform interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that interfaces exist and have expected methods
            var iPlatformAdapterType = typeof(IPlatformAdapter);
            var iRuntimeEnvironmentType = typeof(IRuntimeEnvironment);
            
            var iPlatformAdapterMethods = iPlatformAdapterType.GetMethods();
            var iRuntimeEnvironmentMethods = iRuntimeEnvironmentType.GetMethods();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Platform interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = iPlatformAdapterType.IsInterface && 
                        iRuntimeEnvironmentType.IsInterface &&
                        iPlatformAdapterMethods.Length > 0 &&
                        iRuntimeEnvironmentMethods.Length > 0;
            
            _logger.LogInformation("Platform interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Platform interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Platform model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidatePlatformModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Platform model validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var runtimeExecutionRequest = new RuntimeExecutionRequest
            {
                Code = "print('Hello, World!')",
                WorkingDirectory = "/app",
                EnvironmentVariables = new Dictionary<string, string> { ["ENV1"] = "value1" },
                TimeoutMs = 10000,
                CaptureOutput = true,
                Options = new Dictionary<string, object> { ["opt1"] = 123 }
            };
            var runtimeExecutionResult = new RuntimeExecutionResult
            {
                IsSuccess = true,
                Output = "Hello, World!\n",
                Error = "",
                ExitCode = 0,
                Duration = TimeSpan.FromMilliseconds(1500),
                Data = new Dictionary<string, object> { ["extra"] = "info" }
            };
            var runtimePackage = new RuntimePackage
            {
                Name = "test-package",
                Version = "1.0.0",
                Description = "A test package",
                IsInstalled = true
            };
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Platform model validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = runtimeExecutionRequest.Code == "print('Hello, World!')" &&
                        runtimeExecutionResult.IsSuccess &&
                        runtimePackage.Name == "test-package" &&
                        runtimeExecutionRequest.EnvironmentVariables.Count == 1 &&
                        runtimeExecutionRequest.Options.Count == 1 &&
                        runtimePackage.IsInstalled;
            _logger.LogInformation("Platform model validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Platform model validation");
            return false;
        }
    }
} 
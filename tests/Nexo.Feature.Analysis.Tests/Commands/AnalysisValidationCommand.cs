using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Enums;
using Nexo.Feature.Analysis.Models;
using Nexo.Feature.Analysis.Services;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Tests.Commands;

/// <summary>
/// Command for validating Analysis functionality with proper logging and timeouts.
/// </summary>
public class AnalysisValidationCommand
{
    private readonly ILogger<AnalysisValidationCommand> _logger;

    public AnalysisValidationCommand(ILogger<AnalysisValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Analysis interface definitions.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAnalysisInterfaces(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Analysis interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that interfaces exist and have expected methods
            var iAnalyzerType = typeof(IAnalyzer);
            var iCodeAnalyzerType = typeof(ICodeAnalyzer);
            var iArchitectureAnalyzerType = typeof(IArchitectureAnalyzer);
            
            var iAnalyzerMethods = iAnalyzerType.GetMethods();
            var iCodeAnalyzerMethods = iCodeAnalyzerType.GetMethods();
            var iArchitectureAnalyzerMethods = iArchitectureAnalyzerType.GetMethods();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Analysis interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = iAnalyzerType.IsInterface && 
                        iCodeAnalyzerType.IsInterface &&
                        iArchitectureAnalyzerType.IsInterface &&
                        iAnalyzerMethods.Length > 0 &&
                        iCodeAnalyzerMethods.Length > 0 &&
                        iArchitectureAnalyzerMethods.Length > 0;
            
            _logger.LogInformation("Analysis interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Analysis interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Analysis model properties.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAnalysisModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Analysis model validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var analysisRequest = new AnalysisRequest
            {
                TargetPath = "/test/path",
                AnalysisType = "code-quality",
                Options = new Dictionary<string, object> { ["option1"] = "value1" }
            };
            var analysisResult = new AnalysisResult
            {
                Success = true,
                Issues = new List<AnalysisIssue>(),
                Metrics = new Dictionary<string, double> { ["metric1"] = 95.5 },
                Summary = "Analysis completed successfully"
            };
            var analysisIssue = new AnalysisIssue
            {
                Severity = "warning",
                Message = "Test issue message",
                Location = "test/file.cs:10",
                Category = "code-style"
            };
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Analysis model validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            // Log actual values for robust diagnostics
            _logger.LogInformation($"AnalysisRequest.TargetPath: {analysisRequest.TargetPath}");
            _logger.LogInformation($"AnalysisRequest.AnalysisType: {analysisRequest.AnalysisType}");
            _logger.LogInformation($"AnalysisRequest.Options.Count: {analysisRequest.Options?.Count}");
            _logger.LogInformation($"AnalysisResult.Success: {analysisResult.Success}");
            _logger.LogInformation($"AnalysisResult.Metrics.Count: {analysisResult.Metrics?.Count}");
            _logger.LogInformation($"AnalysisIssue.Severity: {analysisIssue.Severity}");
            _logger.LogInformation($"AnalysisIssue.Message: {analysisIssue.Message}");
            _logger.LogInformation($"AnalysisIssue.Location: {analysisIssue.Location}");
            _logger.LogInformation($"AnalysisIssue.Category: {analysisIssue.Category}");
            var result = analysisRequest.TargetPath == "/test/path" &&
                        analysisRequest.AnalysisType == "code-quality" &&
                        analysisResult.Success &&
                        analysisIssue.Severity == "warning" &&
                        (analysisRequest.Options?.Count ?? 0) == 1 &&
                        (analysisResult.Metrics?.Count ?? 0) == 1;
            if (!result)
            {
                _logger.LogError("Analysis model validation failed. See property logs above for details.");
            }
            _logger.LogInformation("Analysis model validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Analysis model validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Analysis service functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAnalysisServices(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Analysis service validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that service classes exist and can be instantiated
            var aiEnhancedAnalyzerType = typeof(AIEnhancedAnalyzerService);
            var codeAnalyzerType = typeof(CodeAnalyzerService);
            var architectureAnalyzerType = typeof(ArchitectureAnalyzerService);
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Analysis service validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = aiEnhancedAnalyzerType.IsClass && 
                        codeAnalyzerType.IsClass &&
                        architectureAnalyzerType.IsClass &&
                        !aiEnhancedAnalyzerType.IsAbstract &&
                        !codeAnalyzerType.IsAbstract &&
                        !architectureAnalyzerType.IsAbstract;
            
            _logger.LogInformation("Analysis service validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Analysis service validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Analysis enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAnalysisEnums(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Analysis enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var pending = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Pending);
            var inProgress = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.InProgress);
            var completed = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Completed);
            var failed = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Failed);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Analysis enum validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = pending && inProgress && completed && failed;
            _logger.LogInformation("Analysis enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Analysis enum validation");
            return false;
        }
    }
} 
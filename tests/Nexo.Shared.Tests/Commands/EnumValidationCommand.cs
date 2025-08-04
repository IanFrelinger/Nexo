using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Enums;
using System;

namespace Nexo.Shared.Tests.Commands;

/// <summary>
/// Command for validating Shared enums with proper logging and timeouts.
/// </summary>
public class EnumValidationCommand
{
    private readonly ILogger<EnumValidationCommand> _logger;

    public EnumValidationCommand(ILogger<EnumValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates AgentRequestType enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAgentRequestType(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AgentRequestType enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var taskAssignment = Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.TaskAssignment);
            var codeReview = Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.CodeReview);
            var architectureDesign = Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.ArchitectureDesign);
            var testCreation = Enum.IsDefined(typeof(AgentRequestType), AgentRequestType.TestCreation);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AgentRequestType validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = taskAssignment && codeReview && architectureDesign && testCreation;
            _logger.LogInformation("AgentRequestType enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AgentRequestType enum validation");
            return false;
        }
    }

    /// <summary>
    /// Validates AnalysisStatus enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateAnalysisStatus(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AnalysisStatus enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var success = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Success);
            var partialSuccess = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.PartialSuccess);
            var failed = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Failed);
            var cancelled = Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Cancelled);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("AnalysisStatus validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = success && partialSuccess && failed && cancelled;
            _logger.LogInformation("AnalysisStatus enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AnalysisStatus enum validation");
            return false;
        }
    }

    /// <summary>
    /// Validates FeaturePriority enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateFeaturePriority(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting FeaturePriority enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var low = Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.Low);
            var medium = Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.Medium);
            var high = Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.High);
            var critical = Enum.IsDefined(typeof(FeaturePriority), FeaturePriority.Critical);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("FeaturePriority validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = low && medium && high && critical;
            _logger.LogInformation("FeaturePriority enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FeaturePriority enum validation");
            return false;
        }
    }
} 
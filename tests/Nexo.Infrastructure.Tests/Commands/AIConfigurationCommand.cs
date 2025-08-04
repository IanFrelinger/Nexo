using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Services.AI;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Tests.Commands;

/// <summary>
/// Command for testing AI Configuration functionality with proper resource management.
/// </summary>
public class AIConfigurationCommand
{
    private readonly ILogger<AIConfigurationCommand> _logger;

    public AIConfigurationCommand(ILogger<AIConfigurationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests AI Configuration constructor functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestConstructor(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AI Configuration constructor test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var mockLogger = new Moq.Mock<ILogger<AIConfigurationService>>();
            
            var service = new AIConfigurationService(mockLogger.Object);
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Constructor test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = service != null;
            _logger.LogInformation("AI Configuration constructor test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI Configuration constructor test");
            return false;
        }
    }

    /// <summary>
    /// Tests getting AI configuration with timeout protection.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public async Task<bool> TestGetConfigurationAsync(int timeoutMs = 5000)
    {
        _logger.LogInformation("Starting AI Configuration get test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var mockLogger = new Moq.Mock<ILogger<AIConfigurationService>>();
            
            var service = new AIConfigurationService(mockLogger.Object);
            using var cts = new CancellationTokenSource(timeoutMs);
            
            var config = await service.GetConfigurationAsync(cts.Token);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Get configuration test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = config != null && config.Mode == AIMode.Development && config.Model != null;
            _logger.LogInformation("AI Configuration get test completed: {Result}", result);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI Configuration get test was cancelled due to timeout");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI Configuration get test");
            return false;
        }
    }

    /// <summary>
    /// Tests loading configuration for different modes.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public async Task<bool> TestLoadForModeAsync(int timeoutMs = 5000)
    {
        _logger.LogInformation("Starting AI Configuration load for mode test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var mockLogger = new Moq.Mock<ILogger<AIConfigurationService>>();
            
            var service = new AIConfigurationService(mockLogger.Object);
            using var cts = new CancellationTokenSource(timeoutMs);
            
            var config = await service.LoadForModeAsync(AIMode.Production, cts.Token);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Load for mode test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = config != null && config.Mode == AIMode.Production && config.Model?.Name == "gpt-4";
            _logger.LogInformation("AI Configuration load for mode test completed: {Result}", result);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI Configuration load for mode test was cancelled due to timeout");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI Configuration load for mode test");
            return false;
        }
    }

    /// <summary>
    /// Tests getting default configuration for different modes.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestGetDefaultConfiguration(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting AI Configuration default configuration test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var mockLogger = new Moq.Mock<ILogger<AIConfigurationService>>();
            
            var service = new AIConfigurationService(mockLogger.Object);
            var config = service.GetDefaultConfiguration(AIMode.AIHeavy);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Default configuration test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = config != null && 
                        config.Mode == AIMode.AIHeavy && 
                        config.Model?.Name == "gpt-4-turbo" &&
                        config.Model?.MaxInputTokens == 16384;
            
            _logger.LogInformation("AI Configuration default configuration test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI Configuration default configuration test");
            return false;
        }
    }
} 
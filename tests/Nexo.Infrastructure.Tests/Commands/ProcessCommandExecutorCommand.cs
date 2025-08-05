using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Adapters;
using Nexo.Shared.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Infrastructure.Adapters.Command;

namespace Nexo.Infrastructure.Tests.Commands;

/// <summary>
/// Command for testing ProcessCommandExecutor functionality with proper resource management.
/// </summary>
public class ProcessCommandExecutorCommand
{
    private readonly ILogger<ProcessCommandExecutorCommand> _logger;

    public ProcessCommandExecutorCommand(ILogger<ProcessCommandExecutorCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests ProcessCommandExecutor constructor functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestConstructor(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ProcessCommandExecutor constructor test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var mockLogger = new Moq.Mock<ILogger<ProcessCommandExecutor>>();
            
            var executor = new ProcessCommandExecutor(mockLogger.Object);
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Constructor test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = executor != null;
            _logger.LogInformation("ProcessCommandExecutor constructor test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ProcessCommandExecutor constructor test");
            return false;
        }
    }

    /// <summary>
    /// Tests basic command execution with timeout protection.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 10000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public async Task<bool> TestExecuteAsync(int timeoutMs = 10000)
    {
        _logger.LogInformation("Starting ProcessCommandExecutor execute test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var mockLogger = new Moq.Mock<ILogger<ProcessCommandExecutor>>();
            
            var executor = new ProcessCommandExecutor(mockLogger.Object);
            var request = new CommandRequest
            {
                Command = "echo",
                Arguments = "Hello World"
            };

            using var cts = new CancellationTokenSource(timeoutMs);
            
            var result = await executor.ExecuteAsync(request, cts.Token);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Execute test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var success = result != null && result.IsSuccess && result.ExitCode == 0;
            _logger.LogInformation("ProcessCommandExecutor execute test completed: {Result}", success);
            
            return success;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ProcessCommandExecutor execute test was cancelled due to timeout");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ProcessCommandExecutor execute test");
            return false;
        }
    }

    /// <summary>
    /// Tests command availability check with timeout protection.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public async Task<bool> TestIsCommandAvailableAsync(int timeoutMs = 5000)
    {
        _logger.LogInformation("Starting ProcessCommandExecutor command availability test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var mockLogger = new Moq.Mock<ILogger<ProcessCommandExecutor>>();
            
            var executor = new ProcessCommandExecutor(mockLogger.Object);
            using var cts = new CancellationTokenSource(timeoutMs);
            
            var echoAvailable = await executor.IsCommandAvailableAsync("echo", cts.Token);
            var nonexistentAvailable = await executor.IsCommandAvailableAsync("nonexistent-command-xyz123", cts.Token);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Command availability test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = echoAvailable && !nonexistentAvailable;
            _logger.LogInformation("ProcessCommandExecutor command availability test completed: {Result}", result);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ProcessCommandExecutor command availability test was cancelled due to timeout");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ProcessCommandExecutor command availability test");
            return false;
        }
    }
} 
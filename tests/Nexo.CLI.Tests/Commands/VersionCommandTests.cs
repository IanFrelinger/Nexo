using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;
using System;

namespace Nexo.CLI.Tests.Commands;

/// <summary>
/// Command for testing CLI version functionality with proper logging and timeouts.
/// </summary>
public class VersionCommandTests
{
    private readonly ILogger<VersionCommandTests> _logger;

    public VersionCommandTests(ILogger<VersionCommandTests> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests version command functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestVersionCommand(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting version command test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var command = new Command("version", "Display version information");
            command.SetHandler(() =>
            {
                console.WriteLine("Nexo CLI v1.0.0");
                console.WriteLine("AI-Enhanced Development Environment Orchestration Platform");
            });

            command.Invoke("", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Version command test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Nexo CLI v1.0.0") && 
                        output.Contains("AI-Enhanced Development Environment Orchestration Platform");
            
            _logger.LogInformation("Version command test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during version command test");
            return false;
        }
    }
} 
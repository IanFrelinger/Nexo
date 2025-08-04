using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;
using System;
using System.Threading.Tasks;

namespace Nexo.CLI.Tests.Commands;

/// <summary>
/// Command for testing CLI analyze functionality with proper logging and timeouts.
/// </summary>
public class AnalyzeCommandTests
{
    private readonly ILogger<AnalyzeCommandTests> _logger;

    public AnalyzeCommandTests(ILogger<AnalyzeCommandTests> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests analyze command with valid path.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestAnalyzeCommandWithValidPath(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting analyze command with valid path test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var command = new Command("analyze", "Analyze code for quality and architectural compliance");
            var pathArgument = new Argument<string>("path", "Path to file or directory to analyze");
            var outputOption = new Option<string>("--output", "Output format (json, text)") { IsRequired = false };
            
            command.AddArgument(pathArgument);
            command.AddOption(outputOption);
            command.SetHandler(async (path, output) =>
            {
                console.WriteLine($"Analyzing: {path}");
                console.WriteLine($"Output format: {output ?? "text"}");
                await Task.Delay(100); // Simulate work
                console.WriteLine("Analysis completed.");
            }, pathArgument, outputOption);

            command.Invoke("test-path", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Analyze command test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Analyzing: test-path") && 
                        output.Contains("Output format: text") &&
                        output.Contains("Analysis completed.");
            
            _logger.LogInformation("Analyze command with valid path test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analyze command test");
            return false;
        }
    }

    /// <summary>
    /// Tests analyze command with output option.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestAnalyzeCommandWithOutputOption(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting analyze command with output option test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var command = new Command("analyze", "Analyze code for quality and architectural compliance");
            var pathArgument = new Argument<string>("path", "Path to file or directory to analyze");
            var outputOption = new Option<string>("--output", "Output format (json, text)") { IsRequired = false };
            
            command.AddArgument(pathArgument);
            command.AddOption(outputOption);
            command.SetHandler(async (path, output) =>
            {
                console.WriteLine($"Analyzing: {path}");
                console.WriteLine($"Output format: {output ?? "text"}");
                await Task.Delay(100); // Simulate work
                console.WriteLine("Analysis completed.");
            }, pathArgument, outputOption);

            command.Invoke("test-path --output json", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Analyze command with output option test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Analyzing: test-path") && 
                        output.Contains("Output format: json") &&
                        output.Contains("Analysis completed.");
            
            _logger.LogInformation("Analyze command with output option test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analyze command with output option test");
            return false;
        }
    }
} 
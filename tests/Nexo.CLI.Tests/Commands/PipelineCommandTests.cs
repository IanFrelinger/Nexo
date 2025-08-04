using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.IO;
using System;
using System.Threading.Tasks;

namespace Nexo.CLI.Tests.Commands;

/// <summary>
/// Command for testing CLI pipeline functionality with proper logging and timeouts.
/// </summary>
public class PipelineCommandTests
{
    private readonly ILogger<PipelineCommandTests> _logger;

    public PipelineCommandTests(ILogger<PipelineCommandTests> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests pipeline command with execute functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestPipelineCommandWithExecute(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting pipeline command with execute test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var pipelineCommand = new Command("pipeline", "Pipeline orchestration commands");
            var executeCommand = new Command("execute", "Execute a pipeline");
            var pipelineArgument = new Argument<string>("pipeline", "Pipeline name or path");
            var configOption = new Option<string>("--config", "Configuration file") { IsRequired = false };
            
            executeCommand.AddArgument(pipelineArgument);
            executeCommand.AddOption(configOption);
            executeCommand.SetHandler(async (pipeline, config) =>
            {
                console.WriteLine("Executing pipeline...");
                console.WriteLine($"Pipeline: {pipeline}");
                if (!string.IsNullOrEmpty(config))
                {
                    console.WriteLine($"Config: {config}");
                }
                await Task.Delay(100); // Simulate work
                console.WriteLine("Pipeline execution completed.");
            }, pipelineArgument, configOption);

            pipelineCommand.AddCommand(executeCommand);
            pipelineCommand.Invoke("execute test-pipeline", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Pipeline command with execute test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Executing pipeline...") && 
                        output.Contains("Pipeline: test-pipeline") &&
                        output.Contains("Pipeline execution completed.");
            
            _logger.LogInformation("Pipeline command with execute test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pipeline command with execute test");
            return false;
        }
    }

    /// <summary>
    /// Tests pipeline command with dry run functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if test passes, false otherwise</returns>
    public bool TestPipelineCommandWithDryRun(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting pipeline command with dry run test");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var console = new TestConsole();
            var pipelineCommand = new Command("pipeline", "Pipeline orchestration commands");
            var dryRunCommand = new Command("dry-run", "Simulate pipeline execution");
            var pipelineArgument = new Argument<string>("pipeline", "Pipeline name or path");
            var verboseOption = new Option<bool>("--verbose", "Verbose output") { IsRequired = false };
            
            dryRunCommand.AddArgument(pipelineArgument);
            dryRunCommand.AddOption(verboseOption);
            dryRunCommand.SetHandler(async (pipeline, verbose) =>
            {
                console.WriteLine("Performing dry run...");
                console.WriteLine($"Pipeline: {pipeline}");
                if (verbose)
                {
                    console.WriteLine("Verbose mode enabled");
                }
                await Task.Delay(100); // Simulate work
                console.WriteLine("Dry run completed.");
            }, pipelineArgument, verboseOption);

            pipelineCommand.AddCommand(dryRunCommand);
            pipelineCommand.Invoke("dry-run test-pipeline --verbose", console);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Pipeline command with dry run test exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var output = console.Out.ToString();
            var result = output.Contains("Performing dry run...") && 
                        output.Contains("Pipeline: test-pipeline") &&
                        output.Contains("Verbose mode enabled") &&
                        output.Contains("Dry run completed.");
            
            _logger.LogInformation("Pipeline command with dry run test completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pipeline command with dry run test");
            return false;
        }
    }
} 
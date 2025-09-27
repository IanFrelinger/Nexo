using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Commands;

/// <summary>
/// Code optimization command functionality
/// </summary>
public partial class IterationCommands
{
    /// <summary>
    /// Create the iteration optimize command
    /// </summary>
    public Command CreateIterationOptimizeCommand()
    {
        var command = new Command("optimize", "Optimize existing iteration code");
        
        var inputOption = new Option<string>(
            name: "--input",
            description: "Input file path containing iteration code");
        inputOption.IsRequired = true;
        command.AddOption(inputOption);
        
        var platformOption = new Option<string>(
            name: "--platform",
            description: "Target platform for optimization");
        platformOption.SetDefaultValue("auto");
        command.AddOption(platformOption);
        
        var outputOption = new Option<string>(
            name: "--output",
            description: "Output file path (optional)");
        command.AddOption(outputOption);
        
        command.SetHandler(async (string input, string platform, string? output) =>
        {
            await OptimizeIterationCode(input, platform, output);
        }, inputOption, platformOption, outputOption);
        
        return command;
    }

    private async Task OptimizeIterationCode(string input, string platform, string? output)
    {
        try
        {
            Console.WriteLine("⚡ Optimizing iteration code...");
            Console.WriteLine("===============================");
            
            var existingCode = await System.IO.File.ReadAllTextAsync(input);
            var optimizer = _serviceProvider.GetRequiredService<IIterationCodeOptimizer>();
            
            var request = new IterationOptimizationRequest
            {
                ExistingCode = existingCode,
                TargetPlatform = ParsePlatform(platform),
                Requirements = new PerformanceRequirements(),
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            };
            
            var result = await optimizer.OptimizeIterationCodeAsync(request);
            
            Console.WriteLine("Optimization Results:");
            Console.WriteLine("-------------------");
            Console.WriteLine($"Performance Improvement: {result.OptimizationMetrics.PerformanceImprovementPercentage:F1}%");
            Console.WriteLine($"Memory Improvement: {result.OptimizationMetrics.MemoryImprovementPercentage:F1}%");
            Console.WriteLine($"Selected Strategy: {result.SelectedStrategy?.StrategyId}");
            Console.WriteLine();
            
            Console.WriteLine("Optimized Code:");
            Console.WriteLine("--------------");
            Console.WriteLine(result.OptimizedCode);
            
            if (!string.IsNullOrEmpty(output))
            {
                await System.IO.File.WriteAllTextAsync(output, result.OptimizedCode);
                Console.WriteLine($"SUCCESS: Optimized code saved to: {output}");
            }
            
            Console.WriteLine("SUCCESS: Code optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing iteration code");
            Console.WriteLine($"ERROR: Error optimizing iteration code: {ex.Message}");
        }
    }
}

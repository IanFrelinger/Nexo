using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Commands;

/// <summary>
/// Benchmark command functionality
/// </summary>
public partial class IterationCommands
{
    /// <summary>
    /// Create the iteration benchmark command
    /// </summary>
    public Command CreateIterationBenchmarkCommand()
    {
        var command = new Command("benchmark", "Benchmark iteration strategies");
        
        var dataSizeOption = new Option<int>(
            name: "--data-size",
            description: "Data size for benchmarking");
        dataSizeOption.SetDefaultValue(10000);
        command.AddOption(dataSizeOption);
        
        var platformOption = new Option<string>(
            name: "--platform",
            description: "Target platform for benchmarking");
        platformOption.SetDefaultValue("current");
        command.AddOption(platformOption);
        
        var iterationsOption = new Option<int>(
            name: "--iterations",
            description: "Number of benchmark iterations");
        iterationsOption.SetDefaultValue(5);
        command.AddOption(iterationsOption);
        
        command.SetHandler(async (int dataSize, string platform, int iterations) =>
        {
            await BenchmarkStrategies(dataSize, platform, iterations);
        }, dataSizeOption, platformOption, iterationsOption);
        
        return command;
    }

    private async Task BenchmarkStrategies(int dataSize, string platform, int iterations)
    {
        try
        {
            Console.WriteLine($"Running Benchmarking iteration strategies for {dataSize} items...");
            Console.WriteLine("========================================================");
            
            var benchmarker = _serviceProvider.GetRequiredService<IIterationBenchmarker>();
            
            var results = await benchmarker.BenchmarkAllStrategies(dataSize, platform, iterations);
            
            Console.WriteLine("Benchmark Results:");
            Console.WriteLine("-----------------");
            
            foreach (var result in results.OrderBy(r => r.ExecutionTime))
            {
                var status = result.IsRecommended ? "SUCCESS:" : "⚪";
                Console.WriteLine($"{status} {result.StrategyId}");
                Console.WriteLine($"  Execution Time: {result.ExecutionTime:F2}ms");
                Console.WriteLine($"  Memory Usage: {result.MemoryUsageMB:F2}MB");
                Console.WriteLine($"  Performance Score: {result.PerformanceScore:F1}");
                Console.WriteLine($"  Platform: {result.Platform}");
                Console.WriteLine();
            }
            
            Console.WriteLine("SUCCESS: Benchmarking completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error benchmarking strategies");
            Console.WriteLine($"ERROR: Error benchmarking strategies: {ex.Message}");
        }
    }
}

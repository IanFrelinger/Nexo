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
/// Analysis command functionality
/// </summary>
public partial class IterationCommands
{
    /// <summary>
    /// Create the iteration analyze command
    /// </summary>
    public Command CreateIterationAnalyzeCommand()
    {
        var command = new Command("analyze", "Analyze iteration environment and capabilities");
        
        var detailedOption = new Option<bool>(
            name: "--detailed",
            description: "Show detailed analysis including strategy recommendations");
        command.AddOption(detailedOption);
        
        var platformOption = new Option<string>(
            name: "--platform",
            description: "Target platform for analysis (auto, dotnet, unity, web, mobile, server)");
        platformOption.SetDefaultValue("auto");
        command.AddOption(platformOption);
        
        command.SetHandler(async (bool detailed, string platform) =>
        {
            await AnalyzeEnvironment(detailed, platform);
        }, detailedOption, platformOption);
        
        return command;
    }

    private async Task AnalyzeEnvironment(bool detailed, string platform)
    {
        try
        {
            Console.WriteLine("Search Nexo Iteration Environment Analysis");
            Console.WriteLine("=====================================");
            
            var profile = RuntimeEnvironmentDetector.DetectCurrent();
            var analyzer = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
            
            Console.WriteLine($"Platform: {profile.PlatformType}");
            Console.WriteLine($"CPU Cores: {profile.CpuCores}");
            Console.WriteLine($"Available Memory: {profile.AvailableMemoryMB} MB");
            Console.WriteLine($"Constrained Environment: {profile.IsConstrained}");
            Console.WriteLine($"Mobile Environment: {profile.IsMobile}");
            Console.WriteLine($"Web Environment: {profile.IsWeb}");
            Console.WriteLine($"Unity Environment: {profile.IsUnity}");
            Console.WriteLine();
            
            if (detailed)
            {
                await ShowDetailedAnalysis(analyzer, profile);
            }
            
            Console.WriteLine("SUCCESS: Environment analysis completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing environment");
            Console.WriteLine($"ERROR: Error analyzing environment: {ex.Message}");
        }
    }

    private async Task ShowDetailedAnalysis(IIterationStrategySelector analyzer, RuntimeEnvironmentProfile profile)
    {
        Console.WriteLine("Stats Detailed Analysis");
        Console.WriteLine("-------------------");
        
        // Get recommendations for the current platform
        var recommendations = analyzer.GetRecommendations(profile.PlatformType);
        
        Console.WriteLine($"Strategy Recommendations for {profile.PlatformType}:");
        foreach (var recommendation in recommendations)
        {
            Console.WriteLine($"  • {recommendation.Scenario}");
            Console.WriteLine($"    Strategy: {recommendation.RecommendedStrategyId}");
            Console.WriteLine($"    Reasoning: {recommendation.Reasoning}");
            Console.WriteLine($"    Data Size Range: {recommendation.DataSizeRange.Min} - {recommendation.DataSizeRange.Max}");
            Console.WriteLine($"    Performance: {recommendation.PerformanceCharacteristics}");
            Console.WriteLine();
        }
        
        // Show strategy comparison for different scenarios
        var scenarios = new[]
        {
            new { Name = "Small Dataset (100 items)", DataSize = 100 },
            new { Name = "Medium Dataset (1,000 items)", DataSize = 1000 },
            new { Name = "Large Dataset (10,000 items)", DataSize = 10000 },
            new { Name = "Very Large Dataset (100,000 items)", DataSize = 100000 }
        };
        
        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"Strategy Comparison for {scenario.Name}:");
            
            var context = new IterationContext
            {
                DataSize = scenario.DataSize,
                Requirements = new PerformanceRequirements(),
                EnvironmentProfile = profile,
                TargetPlatform = GetPlatformTargetFromProfile(profile)
            };
            
            var comparison = await analyzer.CompareStrategies<object>(context);
            
            foreach (var result in comparison.Take(3))
            {
                var status = result.IsRecommended ? "SUCCESS:" : "⚪";
                Console.WriteLine($"  {status} {result.Strategy.StrategyId}");
                Console.WriteLine($"    Suitability: {result.SuitabilityScore:F1}%");
                Console.WriteLine($"    Performance: {result.PerformanceEstimate.PerformanceScore:F1}");
                Console.WriteLine($"    Reasoning: {result.Reasoning}");
            }
            Console.WriteLine();
        }
    }
}

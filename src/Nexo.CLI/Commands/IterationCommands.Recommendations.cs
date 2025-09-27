using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Commands;

/// <summary>
/// Recommendations command functionality
/// </summary>
public partial class IterationCommands
{
    /// <summary>
    /// Create the iteration recommendations command
    /// </summary>
    public Command CreateIterationRecommendationsCommand()
    {
        var command = new Command("recommendations", "Get iteration strategy recommendations");
        
        var platformOption = new Option<string>(
            name: "--platform",
            description: "Target platform for recommendations");
        platformOption.SetDefaultValue("auto");
        command.AddOption(platformOption);
        
        command.SetHandler(async (string platform) =>
        {
            await ShowRecommendations(platform);
        }, platformOption);
        
        return command;
    }

    private async Task ShowRecommendations(string platform)
    {
        try
        {
            Console.WriteLine("Idea Iteration Strategy Recommendations");
            Console.WriteLine("=====================================");
            
            var analyzer = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
            var platformType = ParsePlatformType(platform);
            
            var recommendations = analyzer.GetRecommendations(platformType);
            
            Console.WriteLine($"Recommendations for {platformType}:");
            Console.WriteLine();
            
            foreach (var recommendation in recommendations)
            {
                Console.WriteLine($"List {recommendation.Scenario}");
                Console.WriteLine($"   Strategy: {recommendation.RecommendedStrategyId}");
                Console.WriteLine($"   Reasoning: {recommendation.Reasoning}");
                Console.WriteLine($"   Data Size Range: {recommendation.DataSizeRange.Min} - {recommendation.DataSizeRange.Max}");
                Console.WriteLine($"   Performance: {recommendation.PerformanceCharacteristics}");
                Console.WriteLine();
            }
            
            Console.WriteLine("SUCCESS: Recommendations completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing recommendations");
            Console.WriteLine($"ERROR: Error showing recommendations: {ex.Message}");
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Recommendations functionality for hardware command.
    /// </summary>
    public partial class HardwareCommand
    {
        /// <summary>
        /// Shows recommendations
        /// </summary>
        private async Task ShowRecommendationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Idea Hardware Recommendations");
                Console.WriteLine();

                var capabilities = await _hardwareChecker.CheckSystemCapabilitiesAsync(cancellationToken);
                var recommendedTier = await _hardwareChecker.GetRecommendedPerformanceTierAsync(cancellationToken);
                var recommendedCloud = await _hardwareChecker.GetRecommendedCloudOptionAsync(cancellationToken);

                if (recommendedTier != null)
                {
                    Console.WriteLine("Target Recommended Performance Tier:");
                    Console.WriteLine($"   {recommendedTier.Name} - {recommendedTier.Description}");
                    Console.WriteLine($"   Level: {recommendedTier.Level}");
                    Console.WriteLine($"   Features: {string.Join(", ", recommendedTier.Features)}");
                    Console.WriteLine();
                }

                if (recommendedCloud != null)
                {
                    Console.WriteLine("Cloud  Recommended Cloud Option:");
                    Console.WriteLine($"   {recommendedCloud.Name} - {recommendedCloud.Description}");
                    Console.WriteLine($"   Provider: {recommendedCloud.Provider}");
                    Console.WriteLine($"   Cost: ${recommendedCloud.Pricing.MonthlyRate:F2}/month");
                    Console.WriteLine($"   Setup: {recommendedCloud.SetupInstructions}");
                    Console.WriteLine();
                }

                var recommendations = await _hardwareChecker.GetOptimizationRecommendationsAsync(cancellationToken);
                if (recommendations.Any())
                {
                    Console.WriteLine("Tool Optimization Recommendations:");
                    foreach (var recommendation in recommendations.OrderBy(r => r.Priority))
                    {
                        var priorityIcon = recommendation.Priority switch
                        {
                            1 => "Hot",
                            2 => "⚡",
                            3 => "Idea",
                            _ => "INFO:"
                        };
                        
                        Console.WriteLine($"   {priorityIcon} {recommendation.Title}");
                        Console.WriteLine($"      {recommendation.Description}");
                        Console.WriteLine($"      Implementation: {recommendation.Implementation}");
                        Console.WriteLine($"      Cost: ${recommendation.Cost:F2} {recommendation.CostDescription}");
                        Console.WriteLine($"      Impact: {recommendation.ImpactScore:F1}/10");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing recommendations");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
            }
        }
    }
}

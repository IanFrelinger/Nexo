using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Cost estimates and performance tiers functionality for hardware command.
    /// </summary>
    public partial class HardwareCommand
    {
        /// <summary>
        /// Shows cost estimates for cloud options
        /// </summary>
        private async Task ShowCostEstimatesAsync(int hoursPerMonth, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"💰 Cloud Cost Estimates ({hoursPerMonth} hours/month)");
                Console.WriteLine();

                var costs = await _hardwareChecker.EstimateCloudCostsAsync(hoursPerMonth, cancellationToken);

                foreach (var kvp in costs.OrderBy(kvp => kvp.Value))
                {
                    Console.WriteLine($"   {kvp.Key}: ${kvp.Value:F2}/month");
                }

                Console.WriteLine();
                Console.WriteLine("Idea Cost Optimization Tips:");
                Console.WriteLine("   • Use spot instances for non-critical workloads");
                Console.WriteLine("   • Implement auto-scaling to reduce idle time");
                Console.WriteLine("   • Consider reserved instances for predictable usage");
                Console.WriteLine("   • Monitor usage and adjust instance sizes accordingly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing cost estimates");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows performance tiers
        /// </summary>
        private async Task ShowPerformanceTiersAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("⚡ Performance Tiers");
                Console.WriteLine();

                var requirements = await _hardwareChecker.GetHardwareRequirementsAsync(cancellationToken);

                foreach (var tier in requirements.PerformanceTiers.OrderBy(t => t.Level))
                {
                    var recommendedIcon = tier.IsRecommended ? "Star" : "  ";
                    Console.WriteLine($"{recommendedIcon} {tier.Name}");
                    Console.WriteLine($"   Level: {tier.Level}");
                    Console.WriteLine($"   Description: {tier.Description}");
                    Console.WriteLine($"   Memory: {FormatBytes(tier.Requirements.MinimumMemoryBytes)}");
                    Console.WriteLine($"   CPU: {tier.Requirements.MinimumCpuCores} cores @ {tier.Requirements.MinimumCpuFrequencyGhz:F1} GHz");
                    Console.WriteLine($"   Storage: {FormatBytes(tier.Requirements.MinimumStorageBytes)}");
                    Console.WriteLine($"   Features: {string.Join(", ", tier.Features)}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing performance tiers");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
            }
        }
    }
}

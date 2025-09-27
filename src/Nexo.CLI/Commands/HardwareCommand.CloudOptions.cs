using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Cloud options functionality for hardware command.
    /// </summary>
    public partial class HardwareCommand
    {
        /// <summary>
        /// Shows cloud fallback options
        /// </summary>
        private async Task ShowCloudOptionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Cloud  Cloud Fallback Options");
                Console.WriteLine();

                var options = await _hardwareChecker.GetCloudFallbackOptionsAsync(cancellationToken);

                foreach (var option in options.Where(opt => opt.IsAvailable))
                {
                    var providerIcon = option.Provider switch
                    {
                        CloudProvider.Azure => "🔵",
                        CloudProvider.AWS => "🟠",
                        CloudProvider.GoogleCloud => "🔴",
                        _ => "Cloud"
                    };

                    Console.WriteLine($"{providerIcon} {option.Name}");
                    Console.WriteLine($"   Provider: {option.Provider}");
                    Console.WriteLine($"   Region: {option.Region}");
                    Console.WriteLine($"   Description: {option.Description}");
                    Console.WriteLine($"   Memory: {FormatBytes(option.Requirements.MinimumMemoryBytes)}");
                    Console.WriteLine($"   CPU: {option.Requirements.MinimumCpuCores} cores");
                    Console.WriteLine($"   Storage: {FormatBytes(option.Requirements.MinimumStorageBytes)}");
                    Console.WriteLine($"   Cost: ${option.Pricing.HourlyRate:F3}/hour (${option.Pricing.MonthlyRate:F2}/month)");
                    Console.WriteLine($"   Features: {string.Join(", ", option.Features)}");
                    Console.WriteLine();
                }

                Console.WriteLine("Idea To get started with cloud fallback:");
                Console.WriteLine("   1. Choose a provider and instance type");
                Console.WriteLine("   2. Follow the setup instructions");
                Console.WriteLine("   3. Install Nexo on the cloud instance");
                Console.WriteLine("   4. Configure your local Nexo to use cloud AI models");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing cloud options");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
            }
        }
    }
}

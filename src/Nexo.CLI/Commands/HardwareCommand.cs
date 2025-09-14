using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Hardware requirements and cloud fallback command
    /// </summary>
    public class HardwareCommand : ICommand
    {
        private readonly IHardwareRequirementsChecker _hardwareChecker;
        private readonly ILogger<HardwareCommand> _logger;

        public HardwareCommand(
            IHardwareRequirementsChecker hardwareChecker,
            ILogger<HardwareCommand> logger)
        {
            _hardwareChecker = hardwareChecker ?? throw new ArgumentNullException(nameof(hardwareChecker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (args.Length == 0)
            {
                await ShowHardwareDashboardAsync(cancellationToken);
                return;
            }

            var command = args[0].ToLowerInvariant();
            switch (command)
            {
                case "check":
                    await CheckSystemRequirementsAsync(cancellationToken);
                    break;

                case "cloud":
                    await ShowCloudOptionsAsync(cancellationToken);
                    break;

                case "recommend":
                    await ShowRecommendationsAsync(cancellationToken);
                    break;

                case "cost":
                    var hours = args.Length > 1 && int.TryParse(args[1], out var h) ? h : 160; // Default 160 hours/month
                    await ShowCostEstimatesAsync(hours, cancellationToken);
                    break;

                case "tiers":
                    await ShowPerformanceTiersAsync(cancellationToken);
                    break;

                default:
                    ShowHelp();
                    break;
            }
        }

        /// <summary>
        /// Shows the hardware requirements dashboard
        /// </summary>
        private async Task ShowHardwareDashboardAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("💻 Nexo Hardware Requirements Dashboard");
                Console.WriteLine();

                var capabilities = await _hardwareChecker.CheckSystemCapabilitiesAsync(cancellationToken);
                
                Console.WriteLine("🖥️  Current System:");
                Console.WriteLine($"   Memory: {FormatBytes(capabilities.AvailableMemoryBytes)}");
                Console.WriteLine($"   CPU: {capabilities.CpuCores} cores @ {capabilities.CpuFrequencyGhz:F1} GHz");
                Console.WriteLine($"   Storage: {FormatBytes(capabilities.AvailableStorageBytes)}");
                Console.WriteLine($"   OS: {capabilities.OperatingSystem}");
                Console.WriteLine($"   Architecture: {capabilities.Architecture}");
                Console.WriteLine($"   GPU: {(capabilities.HasGpu ? capabilities.GpuModel ?? "Unknown" : "None")}");
                Console.WriteLine($"   Network: {(capabilities.HasNetworkConnection ? "Connected" : "Disconnected")}");
                Console.WriteLine();

                // Show capability assessment
                var capabilityIcon = capabilities.OverallCapability switch
                {
                    CapabilityLevel.Excellent => "🌟",
                    CapabilityLevel.Good => "✅",
                    CapabilityLevel.Basic => "⚠️",
                    CapabilityLevel.Minimal => "❌",
                    CapabilityLevel.Insufficient => "🚫",
                    _ => "❓"
                };

                Console.WriteLine($"📊 Capability Assessment:");
                Console.WriteLine($"   Overall: {capabilityIcon} {capabilities.OverallCapability}");
                Console.WriteLine($"   Can Run Nexo: {(capabilities.CanRunNexo ? "✅ Yes" : "❌ No")}");
                Console.WriteLine($"   Cloud Fallback: {(capabilities.CanRunWithCloudFallback ? "✅ Available" : "❌ Not Available")}");
                Console.WriteLine();

                // Show issues
                if (capabilities.Issues.Any())
                {
                    Console.WriteLine("⚠️  Issues Found:");
                    foreach (var issue in capabilities.Issues)
                    {
                        var severityIcon = issue.Severity switch
                        {
                            IssueSeverity.Critical => "🚨",
                            IssueSeverity.High => "⚠️",
                            IssueSeverity.Medium => "ℹ️",
                            IssueSeverity.Low => "✅",
                            _ => "❓"
                        };
                        
                        Console.WriteLine($"   {severityIcon} {issue.Title}");
                        Console.WriteLine($"      {issue.Description}");
                        Console.WriteLine($"      Current: {issue.CurrentValue}, Required: {issue.RequiredValue}");
                        Console.WriteLine($"      Fix: {issue.FixSuggestion}");
                        Console.WriteLine();
                    }
                }

                // Show recommendations
                if (capabilities.Recommendations.Any())
                {
                    Console.WriteLine("💡 Recommendations:");
                    foreach (var recommendation in capabilities.Recommendations.OrderBy(r => r.Priority))
                    {
                        var priorityIcon = recommendation.Priority switch
                        {
                            1 => "🔥",
                            2 => "⚡",
                            3 => "💡",
                            _ => "ℹ️"
                        };
                        
                        Console.WriteLine($"   {priorityIcon} {recommendation.Title}");
                        Console.WriteLine($"      {recommendation.Description}");
                        Console.WriteLine($"      Cost: ${recommendation.Cost:F2} {recommendation.CostDescription}");
                        Console.WriteLine();
                    }
                }

                Console.WriteLine("🛠️  Available Commands:");
                Console.WriteLine("   nexo hardware check        - Check system requirements");
                Console.WriteLine("   nexo hardware cloud       - Show cloud fallback options");
                Console.WriteLine("   nexo hardware recommend   - Show recommendations");
                Console.WriteLine("   nexo hardware cost [hours] - Estimate cloud costs");
                Console.WriteLine("   nexo hardware tiers       - Show performance tiers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing hardware dashboard");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks system requirements
        /// </summary>
        private async Task CheckSystemRequirementsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("🔍 Checking System Requirements");
                Console.WriteLine();

                var capabilities = await _hardwareChecker.CheckSystemCapabilitiesAsync(cancellationToken);
                var requirements = await _hardwareChecker.GetHardwareRequirementsAsync(cancellationToken);

                Console.WriteLine("📋 Requirements Check:");
                Console.WriteLine();

                // Memory check
                var memoryStatus = capabilities.AvailableMemoryBytes >= requirements.MinimumMemoryBytes ? "✅" : "❌";
                Console.WriteLine($"   Memory: {memoryStatus} {FormatBytes(capabilities.AvailableMemoryBytes)} / {FormatBytes(requirements.MinimumMemoryBytes)} (min)");
                if (capabilities.AvailableMemoryBytes >= requirements.RecommendedMemoryBytes)
                {
                    Console.WriteLine($"   Memory: ✅ {FormatBytes(capabilities.AvailableMemoryBytes)} / {FormatBytes(requirements.RecommendedMemoryBytes)} (recommended)");
                }

                // CPU check
                var cpuStatus = capabilities.CpuCores >= requirements.MinimumCpuCores ? "✅" : "❌";
                Console.WriteLine($"   CPU Cores: {cpuStatus} {capabilities.CpuCores} / {requirements.MinimumCpuCores} (min)");
                if (capabilities.CpuCores >= requirements.RecommendedCpuCores)
                {
                    Console.WriteLine($"   CPU Cores: ✅ {capabilities.CpuCores} / {requirements.RecommendedCpuCores} (recommended)");
                }

                var cpuFreqStatus = capabilities.CpuFrequencyGhz >= requirements.MinimumCpuFrequencyGhz ? "✅" : "❌";
                Console.WriteLine($"   CPU Frequency: {cpuFreqStatus} {capabilities.CpuFrequencyGhz:F1} GHz / {requirements.MinimumCpuFrequencyGhz:F1} GHz (min)");

                // Storage check
                var storageStatus = capabilities.AvailableStorageBytes >= requirements.MinimumStorageBytes ? "✅" : "❌";
                Console.WriteLine($"   Storage: {storageStatus} {FormatBytes(capabilities.AvailableStorageBytes)} / {FormatBytes(requirements.MinimumStorageBytes)} (min)");
                if (capabilities.AvailableStorageBytes >= requirements.RecommendedStorageBytes)
                {
                    Console.WriteLine($"   Storage: ✅ {FormatBytes(capabilities.AvailableStorageBytes)} / {FormatBytes(requirements.RecommendedStorageBytes)} (recommended)");
                }

                // OS check
                var osStatus = requirements.SupportedOperatingSystems.Any(os => capabilities.OperatingSystem.Contains(os)) ? "✅" : "⚠️";
                Console.WriteLine($"   Operating System: {osStatus} {capabilities.OperatingSystem}");

                // Architecture check
                var archStatus = requirements.SupportedArchitectures.Contains(capabilities.Architecture) ? "✅" : "❌";
                Console.WriteLine($"   Architecture: {archStatus} {capabilities.Architecture}");

                Console.WriteLine();
                Console.WriteLine($"Overall Status: {(capabilities.CanRunNexo ? "✅ System meets requirements" : "❌ System does not meet requirements")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system requirements");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows cloud fallback options
        /// </summary>
        private async Task ShowCloudOptionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("☁️  Cloud Fallback Options");
                Console.WriteLine();

                var options = await _hardwareChecker.GetCloudFallbackOptionsAsync(cancellationToken);

                foreach (var option in options.Where(opt => opt.IsAvailable))
                {
                    var providerIcon = option.Provider switch
                    {
                        CloudProvider.Azure => "🔵",
                        CloudProvider.AWS => "🟠",
                        CloudProvider.GoogleCloud => "🔴",
                        _ => "☁️"
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

                Console.WriteLine("💡 To get started with cloud fallback:");
                Console.WriteLine("   1. Choose a provider and instance type");
                Console.WriteLine("   2. Follow the setup instructions");
                Console.WriteLine("   3. Install Nexo on the cloud instance");
                Console.WriteLine("   4. Configure your local Nexo to use cloud AI models");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing cloud options");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows recommendations
        /// </summary>
        private async Task ShowRecommendationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("💡 Hardware Recommendations");
                Console.WriteLine();

                var capabilities = await _hardwareChecker.CheckSystemCapabilitiesAsync(cancellationToken);
                var recommendedTier = await _hardwareChecker.GetRecommendedPerformanceTierAsync(cancellationToken);
                var recommendedCloud = await _hardwareChecker.GetRecommendedCloudOptionAsync(cancellationToken);

                if (recommendedTier != null)
                {
                    Console.WriteLine("🎯 Recommended Performance Tier:");
                    Console.WriteLine($"   {recommendedTier.Name} - {recommendedTier.Description}");
                    Console.WriteLine($"   Level: {recommendedTier.Level}");
                    Console.WriteLine($"   Features: {string.Join(", ", recommendedTier.Features)}");
                    Console.WriteLine();
                }

                if (recommendedCloud != null)
                {
                    Console.WriteLine("☁️  Recommended Cloud Option:");
                    Console.WriteLine($"   {recommendedCloud.Name} - {recommendedCloud.Description}");
                    Console.WriteLine($"   Provider: {recommendedCloud.Provider}");
                    Console.WriteLine($"   Cost: ${recommendedCloud.Pricing.MonthlyRate:F2}/month");
                    Console.WriteLine($"   Setup: {recommendedCloud.SetupInstructions}");
                    Console.WriteLine();
                }

                var recommendations = await _hardwareChecker.GetOptimizationRecommendationsAsync(cancellationToken);
                if (recommendations.Any())
                {
                    Console.WriteLine("🔧 Optimization Recommendations:");
                    foreach (var recommendation in recommendations.OrderBy(r => r.Priority))
                    {
                        var priorityIcon = recommendation.Priority switch
                        {
                            1 => "🔥",
                            2 => "⚡",
                            3 => "💡",
                            _ => "ℹ️"
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
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

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
                Console.WriteLine("💡 Cost Optimization Tips:");
                Console.WriteLine("   • Use spot instances for non-critical workloads");
                Console.WriteLine("   • Implement auto-scaling to reduce idle time");
                Console.WriteLine("   • Consider reserved instances for predictable usage");
                Console.WriteLine("   • Monitor usage and adjust instance sizes accordingly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing cost estimates");
                Console.WriteLine($"❌ Error: {ex.Message}");
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
                    var recommendedIcon = tier.IsRecommended ? "⭐" : "  ";
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
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("💻 Nexo Hardware Requirements");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("   nexo hardware                    - Show hardware dashboard");
            Console.WriteLine("   nexo hardware check             - Check system requirements");
            Console.WriteLine("   nexo hardware cloud             - Show cloud fallback options");
            Console.WriteLine("   nexo hardware recommend         - Show recommendations");
            Console.WriteLine("   nexo hardware cost [hours]      - Estimate cloud costs");
            Console.WriteLine("   nexo hardware tiers             - Show performance tiers");
            Console.WriteLine();
        }

        /// <summary>
        /// Formats bytes into human-readable format
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}

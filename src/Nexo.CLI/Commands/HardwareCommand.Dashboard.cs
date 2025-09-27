using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Hardware;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Dashboard functionality for hardware command.
    /// </summary>
    public partial class HardwareCommand
    {
        /// <summary>
        /// Shows the hardware requirements dashboard
        /// </summary>
        private async Task ShowHardwareDashboardAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Computer Nexo Hardware Requirements Dashboard");
                Console.WriteLine();

                var capabilities = await _hardwareChecker.CheckSystemCapabilitiesAsync(cancellationToken);
                
                Console.WriteLine("System:  Current System:");
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
                    CapabilityLevel.Excellent => "EXCELLENT",
                    CapabilityLevel.Good => "SUCCESS:",
                    CapabilityLevel.Basic => "WARNING:",
                    CapabilityLevel.Minimal => "ERROR:",
                    CapabilityLevel.Insufficient => "BLOCKED",
                    _ => "UNKNOWN"
                };

                Console.WriteLine($"Stats Capability Assessment:");
                Console.WriteLine($"   Overall: {capabilityIcon} {capabilities.OverallCapability}");
                Console.WriteLine($"   Can Run Nexo: {(capabilities.CanRunNexo ? "SUCCESS: Yes" : "ERROR: No")}");
                Console.WriteLine($"   Cloud Fallback: {(capabilities.CanRunWithCloudFallback ? "SUCCESS: Available" : "ERROR: Not Available")}");
                Console.WriteLine();

                // Show issues
                if (capabilities.Issues.Any())
                {
                    Console.WriteLine("WARNING:  Issues Found:");
                    foreach (var issue in capabilities.Issues)
                    {
                        var severityIcon = issue.Severity switch
                        {
                            IssueSeverity.Critical => "Alert",
                            IssueSeverity.High => "WARNING:",
                            IssueSeverity.Medium => "INFO:",
                            IssueSeverity.Low => "SUCCESS:",
                            _ => "UNKNOWN"
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
                    Console.WriteLine("Idea Recommendations:");
                    foreach (var recommendation in capabilities.Recommendations.OrderBy(r => r.Priority))
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
                        Console.WriteLine($"      Cost: ${recommendation.Cost:F2} {recommendation.CostDescription}");
                        Console.WriteLine();
                    }
                }

                Console.WriteLine("Commands:  Available Commands:");
                Console.WriteLine("   nexo hardware check        - Check system requirements");
                Console.WriteLine("   nexo hardware cloud       - Show cloud fallback options");
                Console.WriteLine("   nexo hardware recommend   - Show recommendations");
                Console.WriteLine("   nexo hardware cost [hours] - Estimate cloud costs");
                Console.WriteLine("   nexo hardware tiers       - Show performance tiers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing hardware dashboard");
                Console.WriteLine($"ERROR: Error: {ex.Message}");
            }
        }
    }
}

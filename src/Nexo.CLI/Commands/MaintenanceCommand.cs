using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.Maintenance;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Tool maintenance command
    /// </summary>
    public class MaintenanceCommand : ICommand
    {
        private readonly IToolMaintenanceService _maintenanceService;
        private readonly ILogger<MaintenanceCommand> _logger;

        public MaintenanceCommand(
            IToolMaintenanceService maintenanceService,
            ILogger<MaintenanceCommand> logger)
        {
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (args.Length == 0)
            {
                await ShowMaintenanceDashboardAsync(cancellationToken);
                return;
            }

            var command = args[0].ToLowerInvariant();
            switch (command)
            {
                case "analyze":
                    if (args.Length > 1)
                    {
                        await AnalyzeToolAsync(args[1], cancellationToken);
                    }
                    else
                    {
                        Console.WriteLine("❌ Please specify a tool name to analyze");
                    }
                    break;

                case "update":
                    if (args.Length > 1)
                    {
                        await UpdateToolAsync(args[1], cancellationToken);
                    }
                    else
                    {
                        Console.WriteLine("❌ Please specify a tool name to update");
                    }
                    break;

                case "schedule":
                    if (args.Length > 2)
                    {
                        if (DateTime.TryParse(args[2], out var scheduledDate))
                        {
                            await ScheduleMaintenanceAsync(args[1], scheduledDate, cancellationToken);
                        }
                        else
                        {
                            Console.WriteLine("❌ Please specify a valid date for scheduling");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Please specify a tool name and date for scheduling");
                    }
                    break;

                case "stats":
                    await ShowStatisticsAsync(cancellationToken);
                    break;

                case "list":
                    await ListToolsRequiringMaintenanceAsync(cancellationToken);
                    break;

                default:
                    ShowHelp();
                    break;
            }
        }

        /// <summary>
        /// Shows the maintenance dashboard
        /// </summary>
        private async Task ShowMaintenanceDashboardAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("🔧 Nexo Tool Maintenance Dashboard");
                Console.WriteLine();

                var statistics = await _maintenanceService.GetStatisticsAsync(cancellationToken);
                
                Console.WriteLine("📊 Overview:");
                Console.WriteLine($"   Total Tools: {statistics.TotalTools}");
                Console.WriteLine($"   Requiring Maintenance: {statistics.ToolsRequiringMaintenance}");
                Console.WriteLine($"   Critical Issues: {statistics.ToolsWithCriticalIssues}");
                Console.WriteLine($"   Security Issues: {statistics.ToolsWithSecurityIssues}");
                Console.WriteLine($"   Performance Issues: {statistics.ToolsWithPerformanceIssues}");
                Console.WriteLine();

                Console.WriteLine("📈 Maintenance Items:");
                Console.WriteLine($"   Total: {statistics.TotalMaintenanceItems}");
                Console.WriteLine($"   Completed: {statistics.CompletedMaintenanceItems}");
                Console.WriteLine($"   Pending: {statistics.PendingMaintenanceItems}");
                Console.WriteLine($"   Average Time: {statistics.AverageMaintenanceTime:F1} hours");
                Console.WriteLine();

                Console.WriteLine("🛠️  Available Commands:");
                Console.WriteLine("   nexo maintenance analyze <tool-name>  - Analyze a specific tool");
                Console.WriteLine("   nexo maintenance update <tool-name>   - Update a specific tool");
                Console.WriteLine("   nexo maintenance schedule <tool> <date> - Schedule maintenance");
                Console.WriteLine("   nexo maintenance stats                - Show detailed statistics");
                Console.WriteLine("   nexo maintenance list                 - List tools requiring maintenance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing maintenance dashboard");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes a specific tool
        /// </summary>
        private async Task AnalyzeToolAsync(string toolName, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"🔍 Analyzing tool: {toolName}");
                Console.WriteLine();

                var plan = await _maintenanceService.CreateMaintenancePlanAsync(toolName, cancellationToken);

                Console.WriteLine($"📋 Maintenance Plan for {toolName}");
                Console.WriteLine($"   Priority: {plan.Priority}/10");
                Console.WriteLine($"   Status: {plan.Status}");
                Console.WriteLine($"   Estimated Effort: {plan.EstimatedEffort:F1} hours");
                Console.WriteLine($"   Next Due: {plan.NextMaintenanceDue:yyyy-MM-dd}");
                Console.WriteLine();

                // Show dependency updates
                if (plan.DependencyUpdates.Any())
                {
                    Console.WriteLine("📦 Dependency Updates:");
                    foreach (var update in plan.DependencyUpdates)
                    {
                        var icon = update.IsBreakingChange ? "⚠️" : "✅";
                        Console.WriteLine($"   {icon} {update.PackageName}: {update.CurrentVersion} → {update.LatestVersion} ({update.UpdateType})");
                    }
                    Console.WriteLine();
                }

                // Show security updates
                if (plan.SecurityUpdates.Any())
                {
                    Console.WriteLine("🔒 Security Updates:");
                    foreach (var update in plan.SecurityUpdates)
                    {
                        var icon = update.Severity switch
                        {
                            SecuritySeverity.Critical => "🚨",
                            SecuritySeverity.High => "⚠️",
                            SecuritySeverity.Medium => "ℹ️",
                            SecuritySeverity.Low => "✅",
                            _ => "❓"
                        };
                        Console.WriteLine($"   {icon} {update.Title} ({update.Severity})");
                        Console.WriteLine($"      Package: {update.AffectedPackage}");
                        Console.WriteLine($"      Fixed in: {update.FixedVersion}");
                    }
                    Console.WriteLine();
                }

                // Show performance optimizations
                if (plan.PerformanceOptimizations.Any())
                {
                    Console.WriteLine("⚡ Performance Optimizations:");
                    foreach (var optimization in plan.PerformanceOptimizations)
                    {
                        Console.WriteLine($"   🚀 {optimization.Title}");
                        Console.WriteLine($"      Current: {optimization.CurrentPerformance:F1} {optimization.Unit}");
                        Console.WriteLine($"      Expected: {optimization.ExpectedImprovement:F1} {optimization.Unit} improvement");
                    }
                    Console.WriteLine();
                }

                // Show deprecation recommendations
                if (plan.DeprecationRecommendations.Any())
                {
                    Console.WriteLine("📉 Deprecation Recommendations:");
                    foreach (var deprecation in plan.DeprecationRecommendations)
                    {
                        Console.WriteLine($"   ⏰ {deprecation.Title}");
                        Console.WriteLine($"      Reason: {deprecation.Reason}");
                        Console.WriteLine($"      Recommended Date: {deprecation.RecommendedDeprecationDate:yyyy-MM-dd}");
                        if (!string.IsNullOrEmpty(deprecation.ReplacementTool))
                        {
                            Console.WriteLine($"      Replacement: {deprecation.ReplacementTool}");
                        }
                    }
                    Console.WriteLine();
                }

                Console.WriteLine($"✅ Analysis complete. {plan.TotalIssues} issues found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing tool: {ToolName}", toolName);
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a specific tool
        /// </summary>
        private async Task UpdateToolAsync(string toolName, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"🔄 Updating tool: {toolName}");
                Console.WriteLine();

                // Get maintenance plan
                var plan = await _maintenanceService.CreateMaintenancePlanAsync(toolName, cancellationToken);

                if (plan.TotalIssues == 0)
                {
                    Console.WriteLine("✅ No updates needed for this tool.");
                    return;
                }

                // Update dependencies
                if (plan.DependencyUpdates.Any())
                {
                    Console.WriteLine("📦 Updating dependencies...");
                    var depResult = await _maintenanceService.UpdateDependenciesAsync(toolName, plan.DependencyUpdates, cancellationToken);
                    if (depResult.Success)
                    {
                        Console.WriteLine($"   ✅ Dependencies updated successfully ({depResult.Duration:F1}s)");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Dependency update failed: {depResult.Message}");
                    }
                }

                // Apply security updates
                if (plan.SecurityUpdates.Any())
                {
                    Console.WriteLine("🔒 Applying security updates...");
                    var secResult = await _maintenanceService.ApplySecurityUpdatesAsync(toolName, plan.SecurityUpdates, cancellationToken);
                    if (secResult.Success)
                    {
                        Console.WriteLine($"   ✅ Security updates applied successfully ({secResult.Duration:F1}s)");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Security update failed: {secResult.Message}");
                    }
                }

                // Apply performance optimizations
                if (plan.PerformanceOptimizations.Any())
                {
                    Console.WriteLine("⚡ Applying performance optimizations...");
                    var perfResult = await _maintenanceService.ApplyOptimizationsAsync(toolName, plan.PerformanceOptimizations, cancellationToken);
                    if (perfResult.Success)
                    {
                        Console.WriteLine($"   ✅ Performance optimizations applied successfully ({perfResult.Duration:F1}s)");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Performance optimization failed: {perfResult.Message}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("✅ Tool update completed!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tool: {ToolName}", toolName);
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Schedules maintenance for a tool
        /// </summary>
        private async Task ScheduleMaintenanceAsync(string toolName, DateTime scheduledDate, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"📅 Scheduling maintenance for {toolName} on {scheduledDate:yyyy-MM-dd}");
                
                var scheduled = await _maintenanceService.ScheduleMaintenanceAsync(toolName, scheduledDate, cancellationToken);
                
                if (scheduled)
                {
                    Console.WriteLine("✅ Maintenance scheduled successfully!");
                }
                else
                {
                    Console.WriteLine("❌ Failed to schedule maintenance. Tool not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling maintenance for tool: {ToolName}", toolName);
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows detailed statistics
        /// </summary>
        private async Task ShowStatisticsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("📊 Detailed Maintenance Statistics");
                Console.WriteLine();

                var statistics = await _maintenanceService.GetStatisticsAsync(cancellationToken);

                Console.WriteLine("📈 Maintenance by Type:");
                foreach (var kvp in statistics.MaintenanceByType)
                {
                    Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine();

                Console.WriteLine("🎯 Maintenance by Priority:");
                foreach (var kvp in statistics.MaintenanceByPriority)
                {
                    Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine();

                Console.WriteLine($"🕒 Last Maintenance Run: {statistics.LastMaintenanceRun:yyyy-MM-dd HH:mm}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing statistics");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lists tools requiring maintenance
        /// </summary>
        private async Task ListToolsRequiringMaintenanceAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("🔧 Tools Requiring Maintenance");
                Console.WriteLine();

                var tools = await _maintenanceService.GetToolsRequiringMaintenanceAsync(cancellationToken);

                if (!tools.Any())
                {
                    Console.WriteLine("✅ No tools require maintenance at this time.");
                    return;
                }

                foreach (var tool in tools)
                {
                    Console.WriteLine($"   • {tool}");
                }

                Console.WriteLine();
                Console.WriteLine($"Total: {tools.Count} tools requiring maintenance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing tools requiring maintenance");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows help information
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("🔧 Nexo Tool Maintenance");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("   nexo maintenance                     - Show maintenance dashboard");
            Console.WriteLine("   nexo maintenance analyze <tool>     - Analyze a specific tool");
            Console.WriteLine("   nexo maintenance update <tool>      - Update a specific tool");
            Console.WriteLine("   nexo maintenance schedule <tool> <date> - Schedule maintenance");
            Console.WriteLine("   nexo maintenance stats              - Show detailed statistics");
            Console.WriteLine("   nexo maintenance list               - List tools requiring maintenance");
            Console.WriteLine();
        }
    }
}

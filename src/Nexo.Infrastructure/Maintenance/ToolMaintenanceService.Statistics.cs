using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Maintenance;

namespace Nexo.Infrastructure.Maintenance
{
    /// <summary>
    /// Statistics and scheduling functionality
    /// </summary>
    public partial class ToolMaintenanceService
    {
        /// <inheritdoc />
        public async Task<MaintenanceStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting maintenance statistics");

                var statistics = new MaintenanceStatistics
                {
                    TotalTools = _maintenancePlans.Count,
                    ToolsRequiringMaintenance = _maintenancePlans.Values.Count(p => p.TotalIssues > 0),
                    ToolsWithCriticalIssues = _maintenancePlans.Values.Count(p => p.CriticalIssues > 0),
                    ToolsWithSecurityIssues = _maintenancePlans.Values.Count(p => p.SecurityUpdates.Any()),
                    ToolsWithPerformanceIssues = _maintenancePlans.Values.Count(p => p.PerformanceOptimizations.Any()),
                    TotalMaintenanceItems = _maintenancePlans.Values.Sum(p => p.TotalIssues),
                    CompletedMaintenanceItems = _maintenancePlans.Values.Sum(p => p.Items.Count(i => i.Status == MaintenanceStatus.Completed)),
                    PendingMaintenanceItems = _maintenancePlans.Values.Sum(p => p.Items.Count(i => i.Status == MaintenanceStatus.Pending)),
                    AverageMaintenanceTime = _maintenancePlans.Values.Average(p => p.EstimatedEffort),
                    LastMaintenanceRun = DateTime.UtcNow.AddDays(-1)
                };

                // Calculate maintenance by type
                statistics.MaintenanceByType = _maintenancePlans.Values
                    .SelectMany(p => p.Items)
                    .GroupBy(i => i.Type)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                // Calculate maintenance by priority
                statistics.MaintenanceByPriority = _maintenancePlans.Values
                    .SelectMany(p => p.Items)
                    .GroupBy(i => i.Priority)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                _logger.LogDebug("Retrieved maintenance statistics: {TotalTools} tools, {RequiringMaintenance} requiring maintenance",
                    statistics.TotalTools, statistics.ToolsRequiringMaintenance);

                return await Task.FromResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting maintenance statistics");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ScheduleMaintenanceAsync(string toolName, DateTime scheduledDate, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Scheduling maintenance for tool: {ToolName} on {ScheduledDate}", toolName, scheduledDate);

                if (_maintenancePlans.TryGetValue(toolName, out var plan))
                {
                    plan.NextMaintenanceDue = scheduledDate;
                    plan.Status = MaintenanceStatus.Pending;
                    return await Task.FromResult(true);
                }

                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling maintenance for tool: {ToolName}", toolName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<string>> GetToolsRequiringMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting tools requiring maintenance");

                var tools = _maintenancePlans
                    .Where(kvp => kvp.Value.TotalIssues > 0)
                    .OrderByDescending(kvp => kvp.Value.Priority)
                    .ThenByDescending(kvp => kvp.Value.TotalIssues)
                    .Select(kvp => kvp.Key)
                    .ToList();

                _logger.LogDebug("Found {Count} tools requiring maintenance", tools.Count);
                return await Task.FromResult(tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tools requiring maintenance");
                throw;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Maintenance;

namespace Nexo.Infrastructure.Maintenance
{
    /// <summary>
    /// Helper methods and deprecation functionality
    /// </summary>
    public partial class ToolMaintenanceService
    {
        /// <inheritdoc />
        public async Task<List<DeprecationRecommendation>> CheckDeprecationAsync(string toolName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Checking deprecation recommendations for tool: {ToolName}", toolName);

                var recommendations = new List<DeprecationRecommendation>();

                // In a real implementation, this would analyze usage patterns and age
                // For now, we'll simulate some deprecation recommendations
                var simulatedRecommendations = new[]
                {
                    new DeprecationRecommendation
                    {
                        Title = "Legacy API Usage",
                        Description = "This tool uses deprecated APIs that will be removed in the next major version",
                        Reason = DeprecationReason.Outdated,
                        RecommendedDeprecationDate = DateTime.UtcNow.AddMonths(6),
                        ReplacementTool = "modern-api-tool",
                        MigrationPath = "Update to use the new API endpoints",
                        UsageCount = 5,
                        LastUsedAt = DateTime.UtcNow.AddDays(-30),
                        Priority = MaintenancePriority.Medium
                    }
                };

                recommendations.AddRange(simulatedRecommendations);

                _logger.LogDebug("Found {Count} deprecation recommendations for tool: {ToolName}", recommendations.Count, toolName);
                return await Task.FromResult(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking deprecation for tool: {ToolName}", toolName);
                throw;
            }
        }

        #region Private Methods

        private void CalculatePlanMetrics(MaintenancePlan plan)
        {
            plan.TotalIssues = plan.DependencyUpdates.Count + plan.SecurityUpdates.Count + 
                              plan.PerformanceOptimizations.Count + plan.DeprecationRecommendations.Count;

            plan.CriticalIssues = plan.SecurityUpdates.Count(s => s.Severity == SecuritySeverity.Critical) +
                                 plan.DependencyUpdates.Count(d => d.IsBreakingChange && d.Priority == MaintenancePriority.Critical);

            plan.HighPriorityIssues = plan.SecurityUpdates.Count(s => s.Severity == SecuritySeverity.High) +
                                     plan.DependencyUpdates.Count(d => d.Priority == MaintenancePriority.High) +
                                     plan.PerformanceOptimizations.Count(p => p.Priority == MaintenancePriority.High);

            plan.MediumPriorityIssues = plan.SecurityUpdates.Count(s => s.Severity == SecuritySeverity.Medium) +
                                       plan.DependencyUpdates.Count(d => d.Priority == MaintenancePriority.Medium) +
                                       plan.PerformanceOptimizations.Count(p => p.Priority == MaintenancePriority.Medium);

            plan.LowPriorityIssues = plan.SecurityUpdates.Count(s => s.Severity == SecuritySeverity.Low) +
                                    plan.DependencyUpdates.Count(d => d.Priority == MaintenancePriority.Low) +
                                    plan.PerformanceOptimizations.Count(p => p.Priority == MaintenancePriority.Low);

            plan.EstimatedEffort = CalculateEstimatedEffort(plan);
        }

        private double CalculateEstimatedEffort(MaintenancePlan plan)
        {
            var effort = 0.0;

            // Base effort for each type of maintenance
            effort += plan.DependencyUpdates.Count * 0.5; // 30 minutes per dependency update
            effort += plan.SecurityUpdates.Count * 2.0; // 2 hours per security update
            effort += plan.PerformanceOptimizations.Count * 1.5; // 1.5 hours per optimization
            effort += plan.DeprecationRecommendations.Count * 4.0; // 4 hours per deprecation

            return effort;
        }

        private void DeterminePlanPriority(MaintenancePlan plan)
        {
            if (plan.CriticalIssues > 0)
            {
                plan.Priority = 10;
                plan.Status = MaintenanceStatus.Pending;
            }
            else if (plan.HighPriorityIssues > 0)
            {
                plan.Priority = 8;
                plan.Status = MaintenanceStatus.Pending;
            }
            else if (plan.MediumPriorityIssues > 0)
            {
                plan.Priority = 6;
                plan.Status = MaintenanceStatus.Pending;
            }
            else if (plan.LowPriorityIssues > 0)
            {
                plan.Priority = 4;
                plan.Status = MaintenanceStatus.Pending;
            }
            else
            {
                plan.Priority = 1;
                plan.Status = MaintenanceStatus.Completed;
            }

            // Set next maintenance due date based on priority
            plan.NextMaintenanceDue = plan.Priority switch
            {
                >= 8 => DateTime.UtcNow.AddDays(7), // Critical/High: 1 week
                >= 6 => DateTime.UtcNow.AddDays(30), // Medium: 1 month
                >= 4 => DateTime.UtcNow.AddDays(90), // Low: 3 months
                _ => DateTime.UtcNow.AddDays(365) // No issues: 1 year
            };
        }

        #endregion
    }
}

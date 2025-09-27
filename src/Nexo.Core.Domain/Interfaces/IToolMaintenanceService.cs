using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models.Maintenance;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for tool maintenance service
    /// </summary>
    public partial interface IToolMaintenanceService
    {
        /// <summary>
        /// Creates a maintenance plan for a tool
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maintenance plan</returns>
        Task<MaintenancePlan> CreateMaintenancePlanAsync(string toolName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes dependencies for updates
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of dependency updates</returns>
        Task<List<DependencyUpdate>> AnalyzeDependenciesAsync(string toolName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks for security updates
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of security updates</returns>
        Task<List<SecurityUpdate>> CheckSecurityUpdatesAsync(string toolName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Identifies performance optimizations
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of performance optimizations</returns>
        Task<List<PerformanceOptimization>> IdentifyOptimizationsAsync(string toolName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks for deprecation recommendations
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of deprecation recommendations</returns>
        Task<List<DeprecationRecommendation>> CheckDeprecationAsync(string toolName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a tool's dependencies
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="updates">Dependency updates to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Update result</returns>
        Task<MaintenanceResult> UpdateDependenciesAsync(string toolName, List<DependencyUpdate> updates, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies security updates
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="updates">Security updates to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Update result</returns>
        Task<MaintenanceResult> ApplySecurityUpdatesAsync(string toolName, List<SecurityUpdate> updates, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies performance optimizations
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="optimizations">Optimizations to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Update result</returns>
        Task<MaintenanceResult> ApplyOptimizationsAsync(string toolName, List<PerformanceOptimization> optimizations, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets maintenance statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maintenance statistics</returns>
        Task<MaintenanceStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedules maintenance for a tool
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="scheduledDate">When to perform maintenance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if scheduled</returns>
        Task<bool> ScheduleMaintenanceAsync(string toolName, DateTime scheduledDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tools requiring maintenance
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of tools requiring maintenance</returns>
        Task<List<string>> GetToolsRequiringMaintenanceAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Maintenance result
    /// </summary>
    public partial class MaintenanceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public double Duration { get; set; } // in seconds
    }

    /// <summary>
    /// Maintenance statistics
    /// </summary>
    public partial class MaintenanceStatistics
    {
        public int TotalTools { get; set; }
        public int ToolsRequiringMaintenance { get; set; }
        public int ToolsWithCriticalIssues { get; set; }
        public int ToolsWithSecurityIssues { get; set; }
        public int ToolsWithPerformanceIssues { get; set; }
        public int TotalMaintenanceItems { get; set; }
        public int CompletedMaintenanceItems { get; set; }
        public int PendingMaintenanceItems { get; set; }
        public double AverageMaintenanceTime { get; set; } // in hours
        public DateTime LastMaintenanceRun { get; set; }
        public Dictionary<string, int> MaintenanceByType { get; set; } = new();
        public Dictionary<string, int> MaintenanceByPriority { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Maintenance
{
    /// <summary>
    /// Represents a maintenance plan for a tool
    /// </summary>
    public class MaintenancePlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ToolName { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        // Tool information
        public string ToolVersion { get; set; } = "";
        public DateTime ToolCreatedAt { get; set; }
        public int ToolUsageCount { get; set; }
        public DateTime LastUsedAt { get; set; }
        
        // Maintenance items
        public List<MaintenanceItem> Items { get; set; } = new();
        public List<DependencyUpdate> DependencyUpdates { get; set; } = new();
        public List<SecurityUpdate> SecurityUpdates { get; set; } = new();
        public List<PerformanceOptimization> PerformanceOptimizations { get; set; } = new();
        public List<DeprecationRecommendation> DeprecationRecommendations { get; set; } = new();
        
        // Status
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
        public int Priority { get; set; } = 5; // 1-10, 10 being highest priority
        public double EstimatedEffort { get; set; } // in hours
        public DateTime? NextMaintenanceDue { get; set; }
        
        // Metrics
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int HighPriorityIssues { get; set; }
        public int MediumPriorityIssues { get; set; }
        public int LowPriorityIssues { get; set; }
    }

    /// <summary>
    /// Represents a maintenance item
    /// </summary>
    public class MaintenanceItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MaintenanceItemType Type { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public MaintenancePriority Priority { get; set; }
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? AssignedTo { get; set; }
        public double EstimatedEffort { get; set; } // in hours
        public string? Notes { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents a dependency update
    /// </summary>
    public class DependencyUpdate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PackageName { get; set; } = "";
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string UpdateType { get; set; } = ""; // Major, Minor, Patch
        public bool IsBreakingChange { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime AvailableSince { get; set; }
        public MaintenancePriority Priority { get; set; }
        public List<string> AffectedFiles { get; set; } = new();
    }

    /// <summary>
    /// Represents a security update
    /// </summary>
    public class SecurityUpdate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string VulnerabilityId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public SecuritySeverity Severity { get; set; }
        public string AffectedPackage { get; set; } = "";
        public string FixedVersion { get; set; } = "";
        public DateTime PublishedAt { get; set; }
        public bool IsExploitable { get; set; }
        public string? CveId { get; set; }
        public List<string> AffectedFiles { get; set; } = new();
    }

    /// <summary>
    /// Represents a performance optimization
    /// </summary>
    public class PerformanceOptimization
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public OptimizationType Type { get; set; }
        public double CurrentPerformance { get; set; }
        public double ExpectedImprovement { get; set; }
        public string Unit { get; set; } = ""; // ms, MB, etc.
        public MaintenancePriority Priority { get; set; }
        public List<string> AffectedFiles { get; set; } = new();
        public string? Implementation { get; set; }
    }

    /// <summary>
    /// Represents a deprecation recommendation
    /// </summary>
    public class DeprecationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DeprecationReason Reason { get; set; }
        public DateTime RecommendedDeprecationDate { get; set; }
        public string? ReplacementTool { get; set; }
        public string? MigrationPath { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsedAt { get; set; }
        public MaintenancePriority Priority { get; set; }
    }

    /// <summary>
    /// Maintenance status
    /// </summary>
    public enum MaintenanceStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled,
        Deferred
    }

    /// <summary>
    /// Maintenance priority
    /// </summary>
    public enum MaintenancePriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Maintenance item types
    /// </summary>
    public enum MaintenanceItemType
    {
        DependencyUpdate,
        SecurityUpdate,
        PerformanceOptimization,
        BugFix,
        FeatureEnhancement,
        CodeRefactoring,
        DocumentationUpdate,
        TestUpdate,
        Deprecation
    }

    /// <summary>
    /// Security severity levels
    /// </summary>
    public enum SecuritySeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Performance optimization types
    /// </summary>
    public enum OptimizationType
    {
        MemoryUsage,
        ExecutionTime,
        CpuUsage,
        NetworkLatency,
        DiskIO,
        DatabaseQuery,
        AlgorithmComplexity
    }

    /// <summary>
    /// Deprecation reasons
    /// </summary>
    public enum DeprecationReason
    {
        Outdated,
        Superseded,
        Unused,
        SecurityRisk,
        PerformanceIssues,
        MaintenanceOverhead,
        TechnologyChange
    }
}

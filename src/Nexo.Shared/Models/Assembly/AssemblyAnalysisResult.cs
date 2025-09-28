using System;
using System.Collections.Generic;

namespace Nexo.Shared.Models.Assembly
{
    /// <summary>
    /// Represents the result of analyzing an assembly
    /// </summary>
    public class AssemblyAnalysisResult
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; }
        public AnalysisStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public AssemblyMetadata Metadata { get; set; } = new AssemblyMetadata();
        public IReadOnlyList<SecurityIssue> SecurityIssues { get; set; } = new List<SecurityIssue>();
        public IReadOnlyList<PerformanceIssue> PerformanceIssues { get; set; } = new List<PerformanceIssue>();
        public IReadOnlyList<DependencyIssue> DependencyIssues { get; set; } = new List<DependencyIssue>();
        public IReadOnlyList<CodeQualityIssue> CodeQualityIssues { get; set; } = new List<CodeQualityIssue>();
        public IReadOnlyList<CompatibilityIssue> CompatibilityIssues { get; set; } = new List<CompatibilityIssue>();
        public AssemblyMetrics Metrics { get; set; } = new AssemblyMetrics();
        public IReadOnlyList<AnalysisWarning> Warnings { get; set; } = new List<AnalysisWarning>();
    }

    /// <summary>
    /// Represents a security issue found in an assembly
    /// </summary>
    public class SecurityIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SecuritySeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public IReadOnlyList<string> References { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a performance issue found in an assembly
    /// </summary>
    public class PerformanceIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PerformanceSeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public IReadOnlyList<string> References { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a dependency issue found in an assembly
    /// </summary>
    public class DependencyIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DependencySeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public IReadOnlyList<string> References { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a code quality issue found in an assembly
    /// </summary>
    public class CodeQualityIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QualitySeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public IReadOnlyList<string> References { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a compatibility issue found in an assembly
    /// </summary>
    public class CompatibilityIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CompatibilitySeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public IReadOnlyList<string> References { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents metrics about an assembly
    /// </summary>
    public class AssemblyMetrics
    {
        public int TotalTypes { get; set; }
        public int TotalMethods { get; set; }
        public int TotalProperties { get; set; }
        public int TotalFields { get; set; }
        public int TotalEvents { get; set; }
        public int TotalLinesOfCode { get; set; }
        public int TotalILInstructions { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int DepthOfInheritance { get; set; }
        public int Coupling { get; set; }
        public int Cohesion { get; set; }
        public long AssemblySize { get; set; }
        public int ReferenceCount { get; set; }
        public int ResourceCount { get; set; }
        public int CustomAttributeCount { get; set; }
    }

    /// <summary>
    /// Represents an analysis warning
    /// </summary>
    public class AnalysisWarning
    {
        public string Message { get; set; } = string.Empty;
        public WarningLevel Level { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the status of analysis
    /// </summary>
    public enum AnalysisStatus
    {
        Success,
        Partial,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Represents the severity of a security issue
    /// </summary>
    public enum SecuritySeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Represents the severity of a performance issue
    /// </summary>
    public enum PerformanceSeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Represents the severity of a dependency issue
    /// </summary>
    public enum DependencySeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Represents the severity of a code quality issue
    /// </summary>
    public enum QualitySeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Represents the severity of a compatibility issue
    /// </summary>
    public enum CompatibilitySeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }
}

using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Models.Enterprise
{
    /// <summary>
    /// Represents security configuration for enterprise integration.
    /// </summary>
    public class SecurityConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SecurityStandards { get; set; } = new List<string>();
        public Dictionary<string, object> SecurityPolicies { get; set; } = new Dictionary<string, object>();
        public List<string> AuthenticationMethods { get; set; } = new List<string>();
        public List<string> AuthorizationLevels { get; set; } = new List<string>();
        public Dictionary<string, object> EncryptionSettings { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents compliance requirements for automation.
    /// </summary>
    public class ComplianceRequirements
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ComplianceStandards { get; set; } = new List<string>();
        public List<string> RegulatoryRequirements { get; set; } = new List<string>();
        public Dictionary<string, object> CompliancePolicies { get; set; } = new Dictionary<string, object>();
        public List<string> AuditRequirements { get; set; } = new List<string>();
        public Dictionary<string, object> ReportingRequirements { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents governance configuration for enterprise features.
    /// </summary>
    public class GovernanceConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> GovernancePolicies { get; set; } = new List<string>();
        public Dictionary<string, object> ApprovalWorkflows { get; set; } = new Dictionary<string, object>();
        public List<string> Roles { get; set; } = new List<string>();
        public Dictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();
        public List<string> AuditTrails { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents reporting configuration for enterprise reporting.
    /// </summary>
    public class ReportingConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ReportTypes { get; set; } = new List<string>();
        public Dictionary<string, object> ReportTemplates { get; set; } = new Dictionary<string, object>();
        public List<string> DataSources { get; set; } = new List<string>();
        public Dictionary<string, object> SchedulingSettings { get; set; } = new Dictionary<string, object>();
        public List<string> Recipients { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents security validation configuration.
    /// </summary>
    public class SecurityValidationConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> ValidationTypes { get; set; } = new List<string>();
        public Dictionary<string, object> ValidationRules { get; set; } = new Dictionary<string, object>();
        public List<string> SecurityStandards { get; set; } = new List<string>();
        public Dictionary<string, object> ValidationCriteria { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents security export options.
    /// </summary>
    public class SecurityExportOptions
    {
        public string Format { get; set; } = "JSON";
        public List<string> DataTypes { get; set; } = new List<string>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IncludeMetadata { get; set; } = true;
        public bool Encrypt { get; set; } = true;
    }

    /// <summary>
    /// Represents security import data.
    /// </summary>
    public class SecurityImportData
    {
        public string Id { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime ImportedAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of security integration.
    /// </summary>
    public class SecurityIntegrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public List<string> ImplementedFeatures { get; set; } = new List<string>();
        public Dictionary<string, object> SecurityMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime IntegratedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of compliance automation.
    /// </summary>
    public class ComplianceAutomationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AutomationId { get; set; } = string.Empty;
        public List<string> AutomatedProcesses { get; set; } = new List<string>();
        public Dictionary<string, object> ComplianceMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime AutomatedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of governance implementation.
    /// </summary>
    public class GovernanceImplementationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string GovernanceId { get; set; } = string.Empty;
        public List<string> ImplementedPolicies { get; set; } = new List<string>();
        public Dictionary<string, object> GovernanceMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime ImplementedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of reporting system creation.
    /// </summary>
    public class ReportingSystemResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ReportingId { get; set; } = string.Empty;
        public List<string> CreatedReports { get; set; } = new List<string>();
        public Dictionary<string, object> ReportingMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of security validation.
    /// </summary>
    public class SecurityValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ValidationId { get; set; } = string.Empty;
        public double ComplianceScore { get; set; }
        public List<string> PassedChecks { get; set; } = new List<string>();
        public List<string> FailedChecks { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, object> ValidationMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime ValidatedAt { get; set; }
    }

    /// <summary>
    /// Represents security metrics.
    /// </summary>
    public class SecurityMetrics
    {
        public int TotalSecurityEvents { get; set; }
        public int CriticalSecurityEvents { get; set; }
        public int SecurityViolations { get; set; }
        public double SecurityScore { get; set; }
        public double ComplianceScore { get; set; }
        public Dictionary<string, object> CategoryMetrics { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> TrendMetrics { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents security data export.
    /// </summary>
    public class SecurityDataExport
    {
        public string Id { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public long Size { get; set; }
        public int RecordCount { get; set; }
        public DateTime ExportedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the result of security data import.
    /// </summary>
    public class SecurityDataImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime ImportedAt { get; set; }
    }
}

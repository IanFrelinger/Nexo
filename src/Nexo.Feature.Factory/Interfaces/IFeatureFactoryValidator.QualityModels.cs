using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Code quality request
/// </summary>
public record CodeQualityRequest
{
    public List<string> QualityMetrics { get; init; } = new();
    public double MinimumScore { get; init; }
    public List<string> CodeStandards { get; init; } = new();
    public bool IncludeSecurityScanning { get; init; }
    public bool IncludeVulnerabilityScanning { get; init; }
}

/// <summary>
/// Code quality result
/// </summary>
public record CodeQualityResult
{
    public double OverallQualityScore { get; init; }
    public Dictionary<string, double> MetricScores { get; init; } = new();
    public List<CodeIssue> Issues { get; init; } = new();
    public List<SecurityVulnerability> Vulnerabilities { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public bool MeetsStandards { get; init; }
    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Code issue
/// </summary>
public record CodeIssue
{
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// Security vulnerability
/// </summary>
public record SecurityVulnerability
{
    public string VulnerabilityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Mitigation { get; init; } = string.Empty;
    public double RiskScore { get; init; }
}

/// <summary>
/// Security validation request
/// </summary>
public record SecurityValidationRequest
{
    public List<string> SecurityStandards { get; init; } = new();
    public List<string> ComplianceFrameworks { get; init; } = new();
    public bool IncludePenetrationTesting { get; init; }
    public bool IncludeCodeAnalysis { get; init; }
    public Dictionary<string, object> SecurityParameters { get; init; } = new();
}

/// <summary>
/// Security validation result
/// </summary>
public record SecurityValidationResult
{
    public double SecurityScore { get; init; }
    public Dictionary<string, double> StandardScores { get; init; } = new();
    public List<SecurityIssue> Issues { get; init; } = new();
    public List<ComplianceGap> ComplianceGaps { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public bool IsCompliant { get; init; }
    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Security issue
/// </summary>
public record SecurityIssue
{
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Mitigation { get; init; } = string.Empty;
}

/// <summary>
/// Compliance gap
/// </summary>
public record ComplianceGap
{
    public string Framework { get; init; } = string.Empty;
    public string Requirement { get; init; } = string.Empty;
    public string Gap { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Remediation { get; init; } = string.Empty;
}

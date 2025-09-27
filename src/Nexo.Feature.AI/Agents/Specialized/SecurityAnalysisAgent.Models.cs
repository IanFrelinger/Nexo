using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Data models and records for SecurityAnalysisAgent.
/// </summary>
public partial class SecurityAnalysisAgent
{
    // This partial class contains data models and records
    // The actual models are defined below
}

/// <summary>
/// Security analysis result
/// </summary>
public record SecurityAnalysis
{
    public bool HasVulnerabilities { get; init; }
    public double SecurityScore { get; init; }
    public string ComplianceLevel { get; init; } = "Unknown";
    public SecurityVulnerability[] Vulnerabilities { get; init; } = [];
    public string[] Improvements { get; init; } = [];
}

/// <summary>
/// Security vulnerability information
/// </summary>
public record SecurityVulnerability
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = "Medium";
    public string Description { get; init; } = string.Empty;
    public string? Remediation { get; init; }
}

/// <summary>
/// Platform-specific security analysis
/// </summary>
public record PlatformSecurityAnalysis
{
    public PlatformCompatibility Platform { get; init; }
    public string SecureCode { get; init; } = string.Empty;
    public SecurityLevel SecurityLevel { get; init; }
    public SecurityVulnerability[] PlatformSpecificVulnerabilities { get; init; } = [];
}
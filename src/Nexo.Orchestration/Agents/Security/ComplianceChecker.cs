using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Agents.Security;

/// <summary>
/// Checks code and artifacts for compliance with security standards.
/// 
/// Supports multiple compliance standards:
/// - OWASP Top 10 (injection, cryptographic failures)
/// - CWE Top 25 (SQL injection, etc.)
/// - PCI-DSS (cardholder data protection)
/// 
/// Used by SecurityAnalysisAgent for compliance validation.
/// </summary>
public sealed class ComplianceChecker
{
    private readonly ILogger<ComplianceChecker>? _logger;
    private readonly List<string> _enabledStandards;

    public ComplianceChecker(
        ILogger<ComplianceChecker>? logger = null,
        List<string>? enabledStandards = null)
    {
        _logger = logger;
        _enabledStandards = enabledStandards ?? new List<string> { "OWASP", "CWE" };
    }

    public Task<List<ComplianceIssue>> CheckAsync(
        List<SecurityArtifact> artifacts,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ComplianceIssue>();

        foreach (var standard in _enabledStandards)
        {
            var standardIssues = CheckStandard(artifacts, standard);
            issues.AddRange(standardIssues);
        }

        _logger?.LogDebug("Checked {Count} artifacts against {StandardCount} standards, found {IssueCount} issues",
            artifacts.Count, _enabledStandards.Count, issues.Count);

        return Task.FromResult(issues);
    }

    private List<ComplianceIssue> CheckStandard(List<SecurityArtifact> artifacts, string standard)
    {
        var issues = new List<ComplianceIssue>();

        foreach (var artifact in artifacts)
        {
            var artifactIssues = standard switch
            {
                "OWASP" => CheckOWASP(artifact),
                "CWE" => CheckCWE(artifact),
                "PCI-DSS" => CheckPCIDSS(artifact),
                _ => new List<ComplianceIssue>()
            };
            issues.AddRange(artifactIssues);
        }

        return issues;
    }

    private List<ComplianceIssue> CheckOWASP(SecurityArtifact artifact)
    {
        var issues = new List<ComplianceIssue>();
        var content = artifact.Content.ToLowerInvariant();

        // OWASP Top 10 checks
        if (content.Contains("eval(") || content.Contains("exec("))
        {
            issues.Add(new ComplianceIssue
            {
                Standard = "OWASP",
                Rule = "A03:2021 – Injection",
                Description = "Code execution functions detected - potential injection risk",
                Severity = ComplianceSeverity.High
            });
        }

        if (content.Contains("password") && !content.Contains("hash") && !content.Contains("encrypt"))
        {
            issues.Add(new ComplianceIssue
            {
                Standard = "OWASP",
                Rule = "A02:2021 – Cryptographic Failures",
                Description = "Password handling without proper hashing/encryption",
                Severity = ComplianceSeverity.Critical
            });
        }

        return issues;
    }

    private List<ComplianceIssue> CheckCWE(SecurityArtifact artifact)
    {
        var issues = new List<ComplianceIssue>();
        var content = artifact.Content.ToLowerInvariant();

        // CWE Top 25 checks
        if (content.Contains("sql") && content.Contains("+"))
        {
            issues.Add(new ComplianceIssue
            {
                Standard = "CWE",
                Rule = "CWE-89: SQL Injection",
                Description = "SQL string concatenation detected",
                Severity = ComplianceSeverity.High
            });
        }

        return issues;
    }

    private List<ComplianceIssue> CheckPCIDSS(SecurityArtifact artifact)
    {
        var issues = new List<ComplianceIssue>();
        var content = artifact.Content.ToLowerInvariant();

        // PCI-DSS checks
        if (content.Contains("card") && (content.Contains("number") || content.Contains("pan")))
        {
            issues.Add(new ComplianceIssue
            {
                Standard = "PCI-DSS",
                Rule = "Requirement 3: Protect stored cardholder data",
                Description = "Potential cardholder data handling detected",
                Severity = ComplianceSeverity.Critical
            });
        }

        return issues;
    }
}

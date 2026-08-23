using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.Security;

/// <summary>
/// Agent specialized for security analysis and vulnerability scanning.
/// 
/// Responsibilities:
/// - Scans code and artifacts for vulnerabilities using VulnerabilityScanner
/// - Checks compliance with security standards using ComplianceChecker
/// - Performs advanced security analysis using LLM (IModel)
/// - Generates comprehensive security reports
/// - Validates security constraints
/// - Calculates risk scores based on findings
/// 
/// Uses VulnerabilityScanner and ComplianceChecker for automated analysis.
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public sealed class SecurityAnalysisAgent : BaseAgent
{
    private readonly IModel? _model;
    private readonly VulnerabilityScanner? _scanner;
    private readonly ComplianceChecker? _complianceChecker;

    public SecurityAnalysisAgent(
        AgentSpawnSpec spec,
        ILogger<BaseAgent> logger,
        IModel? model = null,
        VulnerabilityScanner? scanner = null,
        ComplianceChecker? complianceChecker = null)
        : base(spec, logger)
    {
        _model = model;
        _scanner = scanner;
        _complianceChecker = complianceChecker;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("SecurityAnalysisAgent {AgentId} initializing", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("SecurityAnalysisAgent {AgentId} dependencies resolved", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencies,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("SecurityAnalysisAgent {AgentId} executing", Spec.AgentId);

        // Extract code/artifacts to analyze from dependencies
        var artifacts = ExtractArtifacts(dependencies ?? new Dictionary<string, object>());
        
        // Perform vulnerability scanning
        var vulnerabilities = _scanner != null
            ? await _scanner.ScanAsync(artifacts, cancellationToken)
            : new List<Vulnerability>();

        // Check compliance
        var complianceResults = _complianceChecker != null
            ? await _complianceChecker.CheckAsync(artifacts, cancellationToken)
            : new List<ComplianceIssue>();

        // Use LLM for advanced analysis if available
        var advancedAnalysis = _model != null
            ? await PerformAdvancedAnalysisAsync(artifacts, vulnerabilities, complianceResults, cancellationToken)
            : null;

        // Generate security report
        var report = new SecurityAnalysisResult
        {
            Vulnerabilities = vulnerabilities,
            ComplianceIssues = complianceResults,
            AdvancedAnalysis = advancedAnalysis,
            AnalyzedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["agentId"] = Spec.AgentId,
                ["artifactCount"] = artifacts.Count,
                ["vulnerabilityCount"] = vulnerabilities.Count,
                ["complianceIssueCount"] = complianceResults.Count
            }
        };

        // Validate constraints
        ValidateSecurityConstraints(report);

        return report;
    }

    private List<SecurityArtifact> ExtractArtifacts(IReadOnlyDictionary<string, object> dependencies)
    {
        var artifacts = new List<SecurityArtifact>();

        foreach (var (key, value) in dependencies)
        {
            if (value is JsonElement jsonElement)
            {
                // Try to extract code
                if (jsonElement.TryGetProperty("code", out var codeProp))
                {
                    artifacts.Add(new SecurityArtifact
                    {
                        Name = key,
                        Type = "code",
                        Content = codeProp.GetString() ?? "",
                        Language = DetermineLanguage(codeProp.GetString() ?? "")
                    });
                }
                else if (jsonElement.TryGetProperty("schema", out var schemaProp))
                {
                    artifacts.Add(new SecurityArtifact
                    {
                        Name = key,
                        Type = "schema",
                        Content = schemaProp.GetRawText(),
                        Language = "json"
                    });
                }
            }
            else if (value is string stringValue)
            {
                artifacts.Add(new SecurityArtifact
                {
                    Name = key,
                    Type = "text",
                    Content = stringValue,
                    Language = "text"
                });
            }
        }

        return artifacts;
    }

    private string DetermineLanguage(string code)
    {
        // Simple language detection
        if (code.Contains("public class") || code.Contains("namespace"))
            return "csharp";
        if (code.Contains("def ") || code.Contains("import "))
            return "python";
        if (code.Contains("function ") || code.Contains("const "))
            return "javascript";
        return "unknown";
    }

    private async Task<AdvancedSecurityAnalysis?> PerformAdvancedAnalysisAsync(
        List<SecurityArtifact> artifacts,
        List<Vulnerability> vulnerabilities,
        List<ComplianceIssue> complianceIssues,
        CancellationToken cancellationToken)
    {
        if (_model == null) return null;

        var prompt = BuildSecurityAnalysisPrompt(artifacts, vulnerabilities, complianceIssues);
        var modelInput = new ModelInput(new List<(string role, string content)>
        {
            ("user", prompt)
        });
        var modelOutput = await _model.CompleteAsync(modelInput, cancellationToken);

        // Parse LLM response (simplified - would need proper JSON parsing)
        return new AdvancedSecurityAnalysis
        {
            Summary = modelOutput.Text,
            RiskScore = CalculateRiskScore(vulnerabilities, complianceIssues),
            Recommendations = ExtractRecommendations(modelOutput.Text)
        };
    }

    private string BuildSecurityAnalysisPrompt(
        List<SecurityArtifact> artifacts,
        List<Vulnerability> vulnerabilities,
        List<ComplianceIssue> complianceIssues)
    {
        return $@"Perform a comprehensive security analysis:

Artifacts Analyzed: {artifacts.Count}
Vulnerabilities Found: {vulnerabilities.Count}
Compliance Issues: {complianceIssues.Count}

Vulnerabilities:
{string.Join("\n", vulnerabilities.Select(v => $"- {v.Type}: {v.Description} (Severity: {v.Severity})"))}

Compliance Issues:
{string.Join("\n", complianceIssues.Select(c => $"- {c.Standard}: {c.Description}"))}

Provide:
1. Overall risk assessment
2. Priority recommendations
3. Best practices to address issues";
    }

    private int CalculateRiskScore(List<Vulnerability> vulnerabilities, List<ComplianceIssue> complianceIssues)
    {
        var score = 0;
        foreach (var vuln in vulnerabilities)
        {
            score += vuln.Severity switch
            {
                VulnerabilitySeverity.Critical => 10,
                VulnerabilitySeverity.High => 7,
                VulnerabilitySeverity.Medium => 4,
                VulnerabilitySeverity.Low => 1,
                _ => 0
            };
        }
        foreach (var issue in complianceIssues)
        {
            score += issue.Severity switch
            {
                ComplianceSeverity.Critical => 8,
                ComplianceSeverity.High => 5,
                ComplianceSeverity.Medium => 2,
                ComplianceSeverity.Low => 1,
                _ => 0
            };
        }
        return Math.Min(100, score);
    }

    private List<string> ExtractRecommendations(string analysisText)
    {
        // Simple extraction - in real implementation would use NLP/parsing
        var recommendations = new List<string>();
        var lines = analysisText.Split('\n');
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("-") || line.Trim().StartsWith("•"))
            {
                recommendations.Add(line.Trim().Substring(1).Trim());
            }
        }
        return recommendations;
    }

    private void ValidateSecurityConstraints(SecurityAnalysisResult report)
    {
        foreach (var constraint in Spec.Constraints)
        {
            if (constraint.Type == "security")
            {
                var criticalVulns = report.Vulnerabilities
                    .Where(v => v.Severity == VulnerabilitySeverity.Critical || v.Severity == VulnerabilitySeverity.High)
                    .ToList();

                if (criticalVulns.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Security constraint violated: {criticalVulns.Count} critical/high vulnerabilities found");
                }
            }
        }
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("SecurityAnalysisAgent {AgentId} shutting down", Spec.AgentId);
        return Task.CompletedTask;
    }
}

using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Domain.Values;
using Nexo.Tools.Assembly;
using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Infrastructure.Analysis.Rules;

/// <summary>
/// Analysis rule for security scanning.
/// </summary>
public class SecurityAnalysisRule : IAnalysisRule
{
    private readonly ILogger<SecurityAnalysisRule> _logger;

    public SecurityAnalysisRule(ILogger<SecurityAnalysisRule> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "SecurityScan";
    public string Description => "Scans assemblies for security vulnerabilities";

    public async Task<IReadOnlyList<Violation>> AnalyzeAsync(
        FileInfo assemblyFile,
        CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();

        try
        {
            var securityTool = new AssemblySecurityScanTool();
            var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

            var securityCall = new ToolCall(
                "assembly.security_scan",
                JsonDocument.Parse($$"""{"path":"{{assemblyFile.FullName}}"}""").RootElement);

            var result = await securityTool.InvokeAsync(securityCall, snapshot, cancellationToken);

            if (result.Payload is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.TryGetProperty("Count", out var countElement) &&
                    countElement.GetInt32() > 0)
                {
                    violations.Add(new Violation
                    {
                        Rule = Name,
                        Message = $"Security scan found {countElement.GetInt32()} finding(s)",
                        FilePath = assemblyFile.FullName,
                        Severity = RiskLevel.High
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Security scan failed for assembly: {Path}",
                assemblyFile.FullName);

            violations.Add(new Violation
            {
                Rule = Name,
                Message = $"Security scan error: {ex.Message}",
                FilePath = assemblyFile.FullName,
                Severity = RiskLevel.Medium
            });
        }

        return violations;
    }
}


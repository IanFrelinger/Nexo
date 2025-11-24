using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Domain.Values;
using Nexo.Tools.Assembly;
using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Infrastructure.Analysis.Rules;

/// <summary>
/// Analysis rule for code quality metrics (cyclomatic complexity, maintainability).
/// </summary>
public class CodeQualityRule : IAnalysisRule
{
    private readonly ILogger<CodeQualityRule> _logger;

    public CodeQualityRule(ILogger<CodeQualityRule> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "CodeQuality";
    public string Description => "Analyzes code quality metrics";

    public async Task<IReadOnlyList<Violation>> AnalyzeAsync(
        FileInfo assemblyFile,
        CancellationToken cancellationToken = default)
    {
        var violations = new List<Violation>();

        try
        {
            var analyzeTool = new AssemblyAnalyzeTool();
            var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

            var analyzeCall = new ToolCall(
                "assembly.analyze",
                JsonDocument.Parse($$"""{"path":"{{assemblyFile.FullName}}"}""").RootElement);

            var result = await analyzeTool.InvokeAsync(analyzeCall, snapshot, cancellationToken);

            // Parse analysis results for quality metrics
            if (result.Payload is System.Text.Json.JsonElement jsonElement)
            {
                // Check for high complexity or low maintainability
                // This is a placeholder - actual implementation would parse detailed metrics
                if (jsonElement.TryGetProperty("Complexity", out var complexityElement))
                {
                    var complexity = complexityElement.GetInt32();
                    if (complexity > 20) // Threshold for high complexity
                    {
                        violations.Add(new Violation
                        {
                            Rule = Name,
                            Message = $"High cyclomatic complexity detected: {complexity}",
                            FilePath = assemblyFile.FullName,
                            Severity = RiskLevel.Medium
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Code quality analysis failed for assembly: {Path}",
                assemblyFile.FullName);
        }

        return violations;
    }
}


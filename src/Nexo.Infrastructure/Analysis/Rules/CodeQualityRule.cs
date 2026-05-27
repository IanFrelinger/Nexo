using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Domain.Values;
using Nexo.Tools.Assembly;
using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Infrastructure.Analysis.Rules;

/// <summary>
/// Analysis rule for code quality metrics (cyclomatic complexity, maintainability).
/// 
/// Responsibilities:
/// - Analyzes code quality metrics using AssemblyAnalyzeTool
/// - Detects high cyclomatic complexity
/// - Reports maintainability issues as violations
/// 
/// Implements IAnalysisRule for use with AnalysisRuleEngine.
/// Used by AnalysisServiceAdapter to assess code quality in assemblies.
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

            // Use JsonSerializer to safely encode the path. Raw string interpolation breaks
            // on Windows paths because the backslashes (\U, \b, \n …) become invalid JSON escapes.
            var argsElement = JsonSerializer.SerializeToElement(new { path = assemblyFile.FullName });
            var analyzeCall = new ToolCall("assembly.analyze", argsElement);

            var result = await analyzeTool.InvokeAsync(analyzeCall, snapshot, cancellationToken);

            // Parse analysis results for quality metrics
            if (TryGetPayloadElement(result.Payload, out var jsonElement))
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
        catch (BadImageFormatException)
        {
            // Native or non-managed file — not a .NET assembly. Skip silently; this is expected
            // when scanning bin/obj output that contains native interop DLLs (e.g. libllama, e_sqlite3).
            _logger.LogDebug(
                "Skipping non-managed assembly: {Path}",
                assemblyFile.FullName);
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

    private static bool TryGetPayloadElement(object? payload, out JsonElement jsonElement)
    {
        if (payload is JsonElement element)
        {
            jsonElement = element;
            return true;
        }

        if (payload is null)
        {
            jsonElement = default;
            return false;
        }

        jsonElement = JsonSerializer.SerializeToElement(payload);
        return true;
    }
}


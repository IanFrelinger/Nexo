using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Tools.Assembly;
using Nexo.Abstractions;
using System.Text.Json;

namespace Nexo.Infrastructure.Analysis.Adapters;

/// <summary>
/// Infrastructure adapter for code/assembly analysis.
/// Implements IAnalysisService port from Application layer.
/// </summary>
public class AnalysisServiceAdapter : IAnalysisService
{
    private readonly ILogger<AnalysisServiceAdapter> _logger;

    public AnalysisServiceAdapter(ILogger<AnalysisServiceAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AnalysisResult> AnalyzeAsync(
        DirectoryInfo path,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Analyzing path: {Path}",
            path.FullName);

        var violations = new List<Violation>();

        try
        {
            // Find all .dll and .exe files in the directory
            var assemblyFiles = path.GetFiles("*.dll", SearchOption.AllDirectories)
                .Concat(path.GetFiles("*.exe", SearchOption.AllDirectories))
                .ToList();

            _logger.LogInformation(
                "Found {Count} assembly file(s) to analyze",
                assemblyFiles.Count);

            // Use assembly analysis tools
            var analyzeTool = new AssemblyAnalyzeTool();
            var securityTool = new AssemblySecurityScanTool();
            var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());

            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    // Analyze assembly metadata
                    var analyzeCall = new ToolCall(
                        "assembly.analyze",
                        JsonDocument.Parse($$"""{"path":"{{assemblyFile.FullName}}"}""").RootElement);

                    var analyzeResult = await analyzeTool.InvokeAsync(analyzeCall, snapshot, cancellationToken);

                    // Security scan
                    var securityCall = new ToolCall(
                        "assembly.security_scan",
                        JsonDocument.Parse($$"""{"path":"{{assemblyFile.FullName}}"}""").RootElement);

                    var securityResult = await securityTool.InvokeAsync(securityCall, snapshot, cancellationToken);

                    // Check for security findings
                    if (securityResult.Payload is System.Text.Json.JsonElement jsonElement)
                    {
                        if (jsonElement.TryGetProperty("Count", out var countElement) &&
                            countElement.GetInt32() > 0)
                        {
                            violations.Add(new Violation
                            {
                                Rule = "SecurityScan",
                                Message = $"Security scan found {countElement.GetInt32()} finding(s)",
                                FilePath = assemblyFile.FullName,
                                Severity = "High"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to analyze assembly: {Path}",
                        assemblyFile.FullName);

                    violations.Add(new Violation
                    {
                        Rule = "AnalysisError",
                        Message = $"Failed to analyze: {ex.Message}",
                        FilePath = assemblyFile.FullName,
                        Severity = "Medium"
                    });
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access to path: {Path}", path.FullName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during analysis");
            throw;
        }

        var result = new AnalysisResult
        {
            HasViolations = violations.Count > 0,
            Violations = violations,
            TotalViolations = violations.Count
        };

        _logger.LogInformation(
            "Analysis completed. Found {Count} violation(s)",
            violations.Count);

        return result;
    }
}

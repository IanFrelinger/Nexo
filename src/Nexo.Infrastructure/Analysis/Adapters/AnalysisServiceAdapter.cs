using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.Exceptions;
using Nexo.Infrastructure.Analysis.Rules;

namespace Nexo.Infrastructure.Analysis.Adapters;

/// <summary>
/// Infrastructure adapter for code/assembly analysis.
/// Implements IAnalysisService port from Application layer.
/// </summary>
public class AnalysisServiceAdapter : IAnalysisService
{
    private readonly ILogger<AnalysisServiceAdapter> _logger;
    private readonly AnalysisRuleEngine _ruleEngine;

    public AnalysisServiceAdapter(
        ILogger<AnalysisServiceAdapter> logger,
        AnalysisRuleEngine ruleEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
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

            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    // Use rule engine to run all analysis rules
                    var ruleViolations = await _ruleEngine.AnalyzeAsync(assemblyFile, cancellationToken);
                    violations.AddRange(ruleViolations);
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
                        Severity = RiskLevel.Medium
                    });
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access to path: {Path}", path.FullName);
            throw new AnalysisException(
                $"Unauthorized access to path: {path.FullName}",
                ex);
        }
        catch (AnalysisException)
        {
            // Re-throw domain exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during analysis");
            throw new AnalysisException(
                $"Analysis failed for path: {path.FullName}",
                ex);
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

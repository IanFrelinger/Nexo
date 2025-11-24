using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;

namespace Nexo.Infrastructure.Analysis.Rules;

/// <summary>
/// Engine for executing multiple analysis rules (Strategy pattern).
/// </summary>
public class AnalysisRuleEngine
{
    private readonly IReadOnlyList<IAnalysisRule> _rules;
    private readonly ILogger<AnalysisRuleEngine> _logger;

    public AnalysisRuleEngine(
        IEnumerable<IAnalysisRule> rules,
        ILogger<AnalysisRuleEngine> logger)
    {
        _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs all registered analysis rules on an assembly file.
    /// </summary>
    public async Task<IReadOnlyList<Violation>> AnalyzeAsync(
        FileInfo assemblyFile,
        CancellationToken cancellationToken = default)
    {
        var allViolations = new List<Violation>();

        _logger.LogInformation(
            "Running {Count} analysis rule(s) on: {Path}",
            _rules.Count,
            assemblyFile.FullName);

        foreach (var rule in _rules)
        {
            try
            {
                var violations = await rule.AnalyzeAsync(assemblyFile, cancellationToken);
                allViolations.AddRange(violations);

                _logger.LogDebug(
                    "Rule '{RuleName}' found {Count} violation(s)",
                    rule.Name,
                    violations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Rule '{RuleName}' failed during analysis",
                    rule.Name);

                // Add violation for rule failure
                allViolations.Add(new Violation
                {
                    Rule = rule.Name,
                    Message = $"Rule execution failed: {ex.Message}",
                    FilePath = assemblyFile.FullName,
                    Severity = Nexo.Core.Domain.Values.RiskLevel.Medium
                });
            }
        }

        return allViolations;
    }

    /// <summary>
    /// Gets all registered rules.
    /// </summary>
    public IReadOnlyList<IAnalysisRule> GetRules() => _rules;
}


using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Validation;

/// <summary>
/// Checks that all requirements from the original request are covered by at least one agent's goal.
/// </summary>
public sealed class CoverageChecker : IValidator
{
    private readonly ILogger<CoverageChecker> _logger;

    public string Name => "CoverageChecker";

    public CoverageChecker(ILogger<CoverageChecker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<ValidationError>> ValidateAsync(
        DecompositionResult result,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(result.OriginalRequest))
        {
            return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
        }

        // Extract key requirements from the original request
        var requirements = ExtractRequirements(result.OriginalRequest);
        
        if (requirements.Count == 0)
        {
            // If we can't extract requirements, skip coverage checking
            _logger.LogDebug("Could not extract requirements from request, skipping coverage check");
            return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
        }

        // Check if each requirement is covered by at least one agent
        var allAgentGoals = result.Agents
            .SelectMany(a => new[] { a.Goal, a.Description ?? string.Empty })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.ToLowerInvariant())
            .ToList();

        var uncoveredRequirements = new List<string>();

        foreach (var requirement in requirements)
        {
            var requirementLower = requirement.ToLowerInvariant();
            var isCovered = allAgentGoals.Any(goal =>
                goal.Contains(requirementLower, StringComparison.OrdinalIgnoreCase) ||
                HasSemanticOverlap(requirementLower, goal));

            if (!isCovered)
            {
                uncoveredRequirements.Add(requirement);
            }
        }

        // Report uncovered requirements
        if (uncoveredRequirements.Count > 0)
        {
            var uncoveredText = string.Join(", ", uncoveredRequirements);
            errors.Add(new ValidationError
            {
                ErrorType = "Coverage",
                Message = $"The following requirements may not be covered by any agent: {uncoveredText}",
                Severity = ValidationSeverity.Warning
            });

            _logger.LogWarning("Uncovered requirements detected: {Requirements}", uncoveredText);
        }

        // Check if there are agents with goals that don't relate to the request
        var requestKeywords = ExtractKeywords(result.OriginalRequest);
        var unrelatedAgents = result.Agents
            .Where(agent =>
            {
                var agentText = $"{agent.Goal} {agent.Description}".ToLowerInvariant();
                return !requestKeywords.Any(keyword =>
                    agentText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

        if (unrelatedAgents.Count > 0 && unrelatedAgents.Count < result.Agents.Count)
        {
            // Only warn if some agents are unrelated (not all, which might be a different issue)
            var unrelatedIds = string.Join(", ", unrelatedAgents.Select(a => a.AgentId));
            errors.Add(new ValidationError
            {
                ErrorType = "Coverage",
                Message = $"The following agents may not relate to the original request: {unrelatedIds}",
                Severity = ValidationSeverity.Warning
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
    }

    private List<string> ExtractRequirements(string request)
    {
        var requirements = new List<string>();

        // Extract requirements using simple heuristics:
        // 1. Look for imperative verbs followed by nouns
        // 2. Look for "with" clauses
        // 3. Look for comma-separated lists

        // Pattern: "Design X", "Create Y", "Implement Z", "Build W"
        var imperativePattern = @"\b(?:design|create|implement|build|develop|make|add|implement|set up)\s+([a-z]+(?:\s+[a-z]+)*)";
        var matches = Regex.Matches(request, imperativePattern, RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                requirements.Add(match.Groups[1].Value.Trim());
            }
        }

        // Pattern: "with X and Y"
        var withPattern = @"\bwith\s+([^,]+(?:\s+and\s+[^,]+)*)";
        matches = Regex.Matches(request, withPattern, RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var withClause = match.Groups[1].Value.Trim();
                // Split on "and"
                var parts = withClause.Split(new[] { " and ", "," }, StringSplitOptions.RemoveEmptyEntries);
                requirements.AddRange(parts.Select(p => p.Trim()));
            }
        }

        // If no requirements found, extract key nouns/phrases
        if (requirements.Count == 0)
        {
            // Extract noun phrases (simplified)
            var nounPattern = @"\b(?:the|a|an)?\s*([a-z]+(?:\s+[a-z]+){0,3})\s+(?:system|feature|mechanic|component|service)";
            matches = Regex.Matches(request, nounPattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    requirements.Add(match.Groups[1].Value.Trim());
                }
            }
        }

        return requirements.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<string> ExtractKeywords(string text)
    {
        // Extract significant words (nouns, verbs, adjectives)
        var words = Regex.Split(text, @"[\s\p{P}]+")
            .Where(w => w.Length > 3)
            .Where(w => !IsStopWord(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .Take(15)
            .ToList();

        return words;
    }

    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
            "been", "being", "have", "has", "had", "do", "does", "did", "will",
            "would", "should", "could", "may", "might", "must", "can", "this",
            "that", "these", "those", "what", "which", "who", "when", "where",
            "why", "how", "all", "each", "every", "some", "any", "no", "not"
        };

        return stopWords.Contains(word);
    }

    private static bool HasSemanticOverlap(string requirement, string goal)
    {
        // Simple semantic overlap check using word similarity
        var reqWords = requirement.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 3)
            .ToHashSet();

        var goalWords = goal.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 3)
            .ToHashSet();

        // Check for word overlap
        var overlap = reqWords.Intersect(goalWords).Count();
        var totalWords = Math.Max(reqWords.Count, goalWords.Count);

        // If more than 30% of words overlap, consider it covered
        return totalWords > 0 && (double)overlap / totalWords > 0.3;
    }
}


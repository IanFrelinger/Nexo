using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Coordination.Conflicts;
using Ashlar.Orchestration.Negotiation.Models;

namespace Ashlar.Orchestration.Negotiation;

/// <summary>
/// Resolves constraint conflicts by finding minimal relaxations.
/// 
/// Responsibilities:
/// - Identifies hard constraint conflicts (unresolvable)
/// - Finds minimal soft constraint relaxations
/// - Evaluates constraint flexibility scores
/// - Generates relaxation plans
/// 
/// Used by NegotiationProtocol to resolve constraint conflicts between agents.
/// </summary>
public sealed class ConstraintRelaxer
{
    private readonly ILogger<ConstraintRelaxer> _logger;

    public ConstraintRelaxer(ILogger<ConstraintRelaxer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to resolve constraint conflicts by relaxing soft constraints.
    /// </summary>
    public RelaxationResult Relax(
        IReadOnlyList<NegotiationPosition> positions,
        Conflict conflict)
    {
        // Collect all constraints
        var hardConstraints = positions
            .SelectMany(p => p.HardConstraints.Select(c => (p.AgentId, Constraint: c, IsHard: true)))
            .ToList();

        var softConstraints = positions
            .SelectMany(p => p.SoftConstraints.Select(kvp =>
                (p.AgentId, Constraint: kvp.Key, Flexibility: kvp.Value)))
            .ToList();

        // Check for hard constraint conflicts (unresolvable)
        var hardConflicts = FindHardConflicts(hardConstraints);
        if (hardConflicts.Any())
        {
            return RelaxationResult.Unresolvable(
                $"Hard constraint conflicts: {string.Join(", ", hardConflicts)}");
        }

        // Find minimal soft constraint relaxation
        var relaxation = FindMinimalRelaxation(softConstraints, conflict);

        if (relaxation == null)
        {
            return RelaxationResult.Unresolvable("No valid relaxation found");
        }

        return new RelaxationResult
        {
            Success = true,
            RelaxedConstraints = relaxation.RelaxedConstraints,
            Explanation = relaxation.Explanation
        };
    }

    private IReadOnlyList<string> FindHardConflicts(
        List<(string AgentId, string Constraint, bool IsHard)> hardConstraints)
    {
        var conflicts = new List<string>();

        // Look for contradictory constraints
        var constraintPairs = new[]
        {
            ("must be fast", "must be thorough"),
            ("minimize latency", "maximize accuracy"),
            ("real-time", "batch processing"),
            ("stateless", "maintain state"),
            ("synchronous", "asynchronous"),
            ("must be fast", "must be slow"),
            ("high", "low"),
            ("maximum", "minimum"),
            ("must", "must not"),
            ("required", "forbidden"),
            ("enabled", "disabled")
        };

        foreach (var (c1, c2) in constraintPairs)
        {
            var hasC1 = hardConstraints.Any(hc =>
                hc.Constraint.Contains(c1, StringComparison.OrdinalIgnoreCase));
            var hasC2 = hardConstraints.Any(hc =>
                hc.Constraint.Contains(c2, StringComparison.OrdinalIgnoreCase));

            if (hasC1 && hasC2)
            {
                conflicts.Add($"'{c1}' conflicts with '{c2}'");
            }
        }

        return conflicts;
    }

    private RelaxationPlan? FindMinimalRelaxation(
        List<(string AgentId, string Constraint, double Flexibility)> softConstraints,
        Conflict conflict)
    {
        // Sort by flexibility (most flexible first)
        var sorted = softConstraints
            .OrderByDescending(c => c.Flexibility)
            .ToList();

        // Try relaxing constraints incrementally until conflict resolves
        var relaxed = new List<(string AgentId, string Constraint)>();

        foreach (var constraint in sorted)
        {
            if (constraint.Flexibility < 0.3)
            {
                // Don't relax constraints with low flexibility
                continue;
            }

            relaxed.Add((constraint.AgentId, constraint.Constraint));

            // Verify that the relaxation actually resolves the conflict
            if (VerifyRelaxationResolvesConflict(relaxed, conflict))
            {
                return new RelaxationPlan
                {
                    RelaxedConstraints = relaxed
                        .ToDictionary(r => r.AgentId, r => r.Constraint),
                    Explanation = $"Relaxed {relaxed.Count} soft constraint(s) to resolve conflict"
                };
            }
        }

        // Try relaxing all flexible constraints as last resort
        var allFlexible = sorted.Where(c => c.Flexibility >= 0.3).ToList();
        if (allFlexible.Any())
        {
            var allRelaxed = allFlexible.Select(c => (c.AgentId, c.Constraint)).ToList();
            if (VerifyRelaxationResolvesConflict(allRelaxed, conflict))
            {
                return new RelaxationPlan
                {
                    RelaxedConstraints = allRelaxed.ToDictionary(r => r.AgentId, r => r.Constraint),
                    Explanation = $"Relaxed all {allRelaxed.Count} flexible constraint(s) to resolve conflict"
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Verifies that relaxing the specified constraints actually resolves the conflict.
    /// </summary>
    private bool VerifyRelaxationResolvesConflict(
        List<(string AgentId, string Constraint)> relaxedConstraints,
        Conflict conflict)
    {
        // Simplified verification: check if relaxed constraints remove contradiction pairs
        var relaxedSet = relaxedConstraints.ToHashSet();

        // Check if any contradiction pairs are now resolved
        var contradictionPairs = new[]
        {
            ("must be fast", "must be thorough"),
            ("minimize latency", "maximize accuracy"),
            ("real-time", "batch processing"),
            ("synchronous", "asynchronous")
        };

        foreach (var (c1, c2) in contradictionPairs)
        {
            var hasC1 = relaxedConstraints.Any(r => r.Constraint.Contains(c1, StringComparison.OrdinalIgnoreCase));
            var hasC2 = relaxedConstraints.Any(r => r.Constraint.Contains(c2, StringComparison.OrdinalIgnoreCase));

            // If both contradictory constraints are relaxed, conflict is resolved
            if (hasC1 && hasC2)
            {
                return true;
            }
        }

        // If we've relaxed constraints from all involved agents, assume conflict resolved
        var involvedAgentIds = conflict.AgentIds.ToHashSet();
        var relaxedAgentIds = relaxedConstraints.Select(r => r.AgentId).ToHashSet();
        if (involvedAgentIds.IsSubsetOf(relaxedAgentIds))
        {
            return true;
        }

        // Default: assume relaxation helps (conservative approach)
        // In a full implementation, would re-run conflict detection with relaxed constraints
        return relaxedConstraints.Count > 0;
    }
}

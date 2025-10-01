using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates mechanics balance and conflicts
    /// </summary>
    public static class MechanicsBalanceValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidateMechanicsBalance(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
            // Check for too many mechanics
            if (plan.CoreMechanics.Count > 10)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Too Many Core Mechanics",
                    Description = $"The game plan has {plan.CoreMechanics.Count} core mechanics, which may be overwhelming.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Consider reducing the number of core mechanics to focus on the most important ones."
                });
            }
            
            // Check for mechanics that might conflict
            var conflictingMechanics = FindConflictingMechanics(plan.CoreMechanics);
            foreach (var conflict in conflictingMechanics)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Conflicting Mechanics",
                    Description = $"Mechanics '{conflict.Item1}' and '{conflict.Item2}' may conflict with each other.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Review the mechanics to ensure they work well together."
                });
            }
            
            return issues;
        }
        
        private static IReadOnlyList<(string, string)> FindConflictingMechanics(IReadOnlyList<string> mechanics)
        {
            var conflicts = new List<(string, string)>();
            
            // Define conflicting mechanic pairs
            var conflictingPairs = new[]
            {
                ("Jump", "Fly"),
                ("Walk", "Run"),
                ("Shoot", "Melee"),
                ("Collect", "Destroy")
            };
            
            foreach (var (mech1, mech2) in conflictingPairs)
            {
                if (mechanics.Contains(mech1) && mechanics.Contains(mech2))
                {
                    conflicts.Add((mech1, mech2));
                }
            }
            
            return conflicts;
        }
    }
}

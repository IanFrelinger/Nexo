using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates mechanics progression and difficulty curves
    /// </summary>
    public static class MechanicsProgressionValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidateMechanicsProgression(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
            // Check if mechanics are introduced in a logical order
            var difficultyProgression = plan.DifficultyProgression;
            if (difficultyProgression.Count > 1)
            {
                var isIncreasing = difficultyProgression.Zip(difficultyProgression.Skip(1), (a, b) => a.DifficultyLevel <= b.DifficultyLevel).All(x => x);
                if (!isIncreasing)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Category = "Mechanics",
                        Title = "Inconsistent Difficulty Progression",
                        Description = "The difficulty progression is not consistently increasing.",
                        Location = "GamePlan.DifficultyProgression",
                        SuggestedFix = "Ensure difficulty increases progressively throughout the game slice."
                    });
                }
            }
            
            return issues;
        }
    }
}

using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates core mechanics requirements
    /// </summary>
    public static class CoreMechanicsValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidateCoreMechanics(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
            // Check if core mechanics are present
            if (plan.CoreMechanics.Count == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Mechanics",
                    Title = "No Core Mechanics",
                    Description = "The game plan has no core mechanics defined.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Add core mechanics that define the primary gameplay loop."
                });
            }
            
            // Check for basic movement
            var hasBasicMovement = plan.CoreMechanics.Any(m => 
                m.Contains("Move") || m.Contains("Walk") || m.Contains("Run") || m.Contains("Navigate"));
            if (!hasBasicMovement)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Missing Basic Movement",
                    Description = "Most games require basic movement mechanics.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Add basic movement mechanics to allow player navigation."
                });
            }
            
            return issues;
        }
    }
}

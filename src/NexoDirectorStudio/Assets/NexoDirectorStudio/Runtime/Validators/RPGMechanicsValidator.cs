using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates RPG-specific mechanics
    /// </summary>
    public static class RPGMechanicsValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidateRPGMechanics(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
            // Check for character progression
            var hasProgression = plan.CoreMechanics.Any(m => m.Contains("Level") || m.Contains("Progress") || m.Contains("Experience"));
            if (!hasProgression)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Missing Progression Mechanics",
                    Description = "RPG games typically include character progression mechanics.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Consider adding progression mechanics to enhance the RPG experience."
                });
            }
            
            // Check for quest mechanics
            var hasQuests = plan.CoreMechanics.Any(m => m.Contains("Quest") || m.Contains("Mission"));
            if (!hasQuests)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Missing Quest Mechanics",
                    Description = "RPG games often include quest mechanics.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Consider adding quest mechanics to provide structure."
                });
            }
            
            // Check for inventory mechanics
            var hasInventory = plan.CoreMechanics.Any(m => m.Contains("Inventory") || m.Contains("Item"));
            if (!hasInventory)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Mechanics",
                    Title = "Missing Inventory Mechanics",
                    Description = "RPG games often include inventory mechanics.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Consider adding inventory mechanics to enhance the RPG experience."
                });
            }
            
            return issues;
        }
    }
}

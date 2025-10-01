using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates platformer-specific mechanics
    /// </summary>
    public static class PlatformerMechanicsValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidatePlatformerMechanics(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
            // Check for jumping mechanics
            var hasJumping = plan.CoreMechanics.Any(m => m.Contains("Jump") || m.Contains("Jumping"));
            if (!hasJumping)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Mechanics",
                    Title = "Missing Jumping Mechanics",
                    Description = "Platformer games require jumping mechanics, but none were found.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Add jumping mechanics to the core mechanics list."
                });
            }
            
            // Check for platform mechanics
            var hasPlatforms = plan.RequiredAssets.Any(a => a.AssetType == "Platform" || a.Name.Contains("Platform"));
            if (!hasPlatforms)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Missing Platform Assets",
                    Description = "Platformer games typically require platform assets.",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Add platform assets to the required assets list."
                });
            }
            
            // Check for collectible mechanics
            var hasCollectibles = plan.CoreMechanics.Any(m => m.Contains("Collect") || m.Contains("Gather"));
            if (!hasCollectibles)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Missing Collectible Mechanics",
                    Description = "Platformer games often include collectible mechanics.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Consider adding collectible mechanics to enhance gameplay."
                });
            }
            
            return issues;
        }
    }
}

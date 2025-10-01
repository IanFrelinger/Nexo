using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates FPS-specific mechanics
    /// </summary>
    public static class FPSMechanicsValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidateFPSMechanics(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
            // Check for shooting mechanics
            var hasShooting = plan.CoreMechanics.Any(m => m.Contains("Shoot") || m.Contains("Aim"));
            if (!hasShooting)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Mechanics",
                    Title = "Missing Shooting Mechanics",
                    Description = "FPS games require shooting mechanics, but none were found.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Add shooting mechanics to the core mechanics list."
                });
            }
            
            // Check for movement mechanics
            var hasMovement = plan.CoreMechanics.Any(m => m.Contains("Move") || m.Contains("Walk") || m.Contains("Run"));
            if (!hasMovement)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Mechanics",
                    Title = "Missing Movement Mechanics",
                    Description = "FPS games require movement mechanics, but none were found.",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = "Add movement mechanics to the core mechanics list."
                });
            }
            
            // Check for weapon mechanics
            var hasWeapon = plan.RequiredAssets.Any(a => a.AssetType == "Weapon" || a.Name.Contains("Weapon"));
            if (!hasWeapon)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Mechanics",
                    Title = "Missing Weapon Assets",
                    Description = "FPS games typically require weapon assets.",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Add weapon assets to the required assets list."
                });
            }
            
            return issues;
        }
    }
}

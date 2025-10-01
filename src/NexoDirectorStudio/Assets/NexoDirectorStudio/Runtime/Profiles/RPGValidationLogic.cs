using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Validation and suggestion logic for RPG genre
    /// </summary>
    public static class RPGValidationLogic
    {
        public static IReadOnlyList<ValidationSuggestion> GetSuggestions(GamePlan plan)
        {
            var suggestions = new List<ValidationSuggestion>();
            
            // Check for missing RPG mechanics
            var requiredMechanics = new[] { "Talk", "Quest", "Level Up", "Inventory" };
            var missingMechanics = requiredMechanics.Where(m => !plan.CoreMechanics.Any(cm => cm.Contains(m))).ToList();
            
            if (missingMechanics.Count > 0)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Mechanics",
                    Title = "Add Missing RPG Mechanics",
                    Description = $"Consider adding missing RPG mechanics: {string.Join(", ", missingMechanics)}",
                    Priority = 4,
                    Effort = "Medium"
                });
            }
            
            // Add asset suggestions
            suggestions.AddRange(RPGAssetRequirements.GetAssetSuggestions(plan));
            
            // Check for appropriate difficulty progression
            if (plan.DifficultyProgression.Count == 0)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Pacing",
                    Title = "Add Difficulty Progression",
                    Description = "RPG games benefit from gradual difficulty progression",
                    Priority = 3,
                    Effort = "Low"
                });
            }
            
            // Check for story elements
            var hasStoryElements = plan.PlayerExperience.Any(pe => pe.Contains("Story") || pe.Contains("Narrative"));
            if (!hasStoryElements)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Content",
                    Title = "Add Story Elements",
                    Description = "RPG games benefit from strong narrative elements",
                    Priority = 3,
                    Effort = "High"
                });
            }
            
            return suggestions;
        }
        
        public static ValidationResult ValidateGenreRequirements(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            var score = 100;
            
            // Check for required mechanics
            var requiredMechanics = new[] { "Talk", "Quest", "Level Up", "Inventory" };
            var missingMechanics = requiredMechanics.Where(m => !plan.CoreMechanics.Any(cm => cm.Contains(m))).ToList();
            
            foreach (var mechanic in missingMechanics)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Mechanics",
                    Title = $"Missing Required RPG Mechanic: {mechanic}",
                    Description = $"RPG games require {mechanic} mechanics for core gameplay",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = $"Add {mechanic} to the core mechanics list"
                });
                score -= 20;
            }
            
            // Add asset validation issues
            var assetIssues = RPGAssetRequirements.ValidateAssetRequirements(plan);
            issues.AddRange(assetIssues);
            score -= assetIssues.Sum(i => i.Severity == ValidationSeverity.Critical ? 20 : 10);
            
            // Check for appropriate difficulty level
            if (plan.SourceBrief.DifficultyLevel > 3)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Difficulty",
                    Title = "High Difficulty for RPG",
                    Description = "RPG games typically have moderate difficulty levels",
                    Location = "GamePlan.SourceBrief.DifficultyLevel",
                    SuggestedFix = "Consider reducing the difficulty level for RPG gameplay"
                });
                score -= 5;
            }
            
            // Check for appropriate duration
            if (plan.EstimatedDurationMinutes < 5)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Duration",
                    Title = "Short Duration for RPG",
                    Description = "RPG games typically require longer duration for story and progression",
                    Location = "GamePlan.EstimatedDurationMinutes",
                    SuggestedFix = "Consider increasing the duration for RPG gameplay"
                });
                score -= 5;
            }
            
            // Check for story elements
            var hasStoryElements = plan.PlayerExperience.Any(pe => pe.Contains("Story") || pe.Contains("Narrative"));
            if (!hasStoryElements)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Content",
                    Title = "Missing Story Elements",
                    Description = "RPG games benefit from strong narrative elements",
                    Location = "GamePlan.PlayerExperience",
                    SuggestedFix = "Consider adding story or narrative elements to the player experience"
                });
                score -= 5;
            }
            
            var message = issues.Count == 0 
                ? "RPG genre requirements validated successfully"
                : $"RPG genre validation found {issues.Count} issues";
            
            return new ValidationResult
            {
                IsValid = issues.Count == 0 || issues.All(i => i.Severity < ValidationSeverity.Critical),
                Score = System.Math.Max(0, score),
                Message = message,
                Details = GenerateRPGDetails(plan, issues),
                Issues = issues,
                Suggestions = GetSuggestions(plan)
            };
        }
        
        private static string GenerateRPGDetails(GamePlan plan, IReadOnlyList<ValidationIssue> issues)
        {
            var details = new List<string>
            {
                $"RPG Genre Validation",
                $"Core Mechanics: {plan.CoreMechanics.Count}",
                $"Required Assets: {plan.RequiredAssets.Count}",
                $"Difficulty Level: {plan.SourceBrief.DifficultyLevel}",
                $"Duration: {plan.EstimatedDurationMinutes} minutes"
            };
            
            if (plan.CoreMechanics.Count > 0)
            {
                details.Add($"Mechanics: {string.Join(", ", plan.CoreMechanics)}");
            }
            
            if (issues.Count > 0)
            {
                details.Add($"Issues Found: {issues.Count}");
                foreach (var issue in issues)
                {
                    details.Add($"- {issue.Severity}: {issue.Title}");
                }
            }
            
            return string.Join("\n", details);
        }
    }
}

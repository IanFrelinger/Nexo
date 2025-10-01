using NexoDirectorStudio.DTO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates that a game slice has appropriate mechanics for its genre.
    /// Ensures genre-specific affordances are present and properly configured.
    /// </summary>
    public sealed class MechanicsValidator : IValidator<GamePlan>
    {
        public async ValueTask<ValidationResult> ValidateAsync(GamePlan input, CancellationToken ct)
        {
            await Task.Delay(50, ct); // Simulate processing time
            
            var issues = new List<ValidationIssue>();
            var suggestions = new List<ValidationSuggestion>();
            var score = 100;
            
            // Validate genre-specific mechanics
            var genreIssues = ValidateGenreMechanics(input);
            issues.AddRange(genreIssues);
            score -= genreIssues.Count * 10;
            
            // Validate core mechanics presence
            var coreMechanicsIssues = CoreMechanicsValidator.ValidateCoreMechanics(input);
            issues.AddRange(coreMechanicsIssues);
            score -= coreMechanicsIssues.Count * 15;
            
            // Validate mechanics balance
            var balanceIssues = MechanicsBalanceValidator.ValidateMechanicsBalance(input);
            issues.AddRange(balanceIssues);
            score -= balanceIssues.Count * 5;
            
            // Validate mechanics progression
            var progressionIssues = MechanicsProgressionValidator.ValidateMechanicsProgression(input);
            issues.AddRange(progressionIssues);
            score -= progressionIssues.Count * 8;
            
            // Generate suggestions
            suggestions.AddRange(MechanicsSuggestionGenerator.GenerateMechanicsSuggestions(input));
            
            var message = issues.Count == 0 
                ? "Mechanics validation passed. The game slice has appropriate mechanics for its genre."
                : $"Mechanics validation found {issues.Count} issues with the game mechanics.";
            
            return new ValidationResult
            {
                IsValid = issues.Count == 0 || issues.All(i => i.Severity < ValidationSeverity.Critical),
                Score = System.Math.Max(0, score),
                Message = message,
                Details = GenerateMechanicsDetails(input, issues),
                Issues = issues,
                Suggestions = suggestions
            };
        }
        
        public async ValueTask<ValidationResult> ValidateAsync(object input, CancellationToken ct)
        {
            if (input is GamePlan plan)
            {
                return await ValidateAsync(plan, ct);
            }
            
            return new ValidationResult
            {
                IsValid = false,
                Score = 0,
                Message = "Invalid input type for mechanics validation.",
                Details = "MechanicsValidator requires a GamePlan input."
            };
        }
        
        private static IReadOnlyList<ValidationIssue> ValidateGenreMechanics(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
            switch (plan.Genre)
            {
                case "FPS":
                    issues.AddRange(FPSMechanicsValidator.ValidateFPSMechanics(plan));
                    break;
                case "Platformer":
                    issues.AddRange(PlatformerMechanicsValidator.ValidatePlatformerMechanics(plan));
                    break;
                case "RPG":
                    issues.AddRange(RPGMechanicsValidator.ValidateRPGMechanics(plan));
                    break;
                default:
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Category = "Mechanics",
                        Title = "Unknown Genre",
                        Description = $"Genre '{plan.Genre}' is not recognized. Using generic validation.",
                        Location = "GamePlan.Genre",
                        SuggestedFix = "Use a recognized genre (FPS, Platformer, RPG) for better validation."
                    });
                    break;
            }
            
            return issues;
        }
        
        private static string GenerateMechanicsDetails(GamePlan plan, IReadOnlyList<ValidationIssue> issues)
        {
            var details = new List<string>
            {
                $"Genre: {plan.Genre}",
                $"Core Mechanics: {plan.CoreMechanics.Count}",
                $"Required Assets: {plan.RequiredAssets.Count}",
                $"Difficulty Progression: {plan.DifficultyProgression.Count} beats"
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
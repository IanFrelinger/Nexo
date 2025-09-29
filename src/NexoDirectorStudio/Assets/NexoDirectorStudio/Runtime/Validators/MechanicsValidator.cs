using NexoDirectorStudio.DTO;
using UnityEngine;

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
            var coreMechanicsIssues = ValidateCoreMechanics(input);
            issues.AddRange(coreMechanicsIssues);
            score -= coreMechanicsIssues.Count * 15;
            
            // Validate mechanics balance
            var balanceIssues = ValidateMechanicsBalance(input);
            issues.AddRange(balanceIssues);
            score -= balanceIssues.Count * 5;
            
            // Validate mechanics progression
            var progressionIssues = ValidateMechanicsProgression(input);
            issues.AddRange(progressionIssues);
            score -= progressionIssues.Count * 8;
            
            // Generate suggestions
            suggestions.AddRange(GenerateMechanicsSuggestions(input));
            
            var message = issues.Count == 0 
                ? "Mechanics validation passed. The game slice has appropriate mechanics for its genre."
                : $"Mechanics validation found {issues.Count} issues with the game mechanics.";
            
            return new ValidationResult
            {
                IsValid = issues.Count == 0 || issues.All(i => i.Severity < ValidationSeverity.Critical),
                Score = Math.Max(0, score),
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
                    issues.AddRange(ValidateFPSMechanics(plan));
                    break;
                case "Platformer":
                    issues.AddRange(ValidatePlatformerMechanics(plan));
                    break;
                case "RPG":
                    issues.AddRange(ValidateRPGMechanics(plan));
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
        
        private static IReadOnlyList<ValidationIssue> ValidateFPSMechanics(GamePlan plan)
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
        
        private static IReadOnlyList<ValidationIssue> ValidatePlatformerMechanics(GamePlan plan)
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
        
        private static IReadOnlyList<ValidationIssue> ValidateRPGMechanics(GamePlan plan)
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
        
        private static IReadOnlyList<ValidationIssue> ValidateCoreMechanics(GamePlan plan)
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
        
        private static IReadOnlyList<ValidationIssue> ValidateMechanicsBalance(GamePlan plan)
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
        
        private static IReadOnlyList<ValidationIssue> ValidateMechanicsProgression(GamePlan plan)
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
        
        private static IReadOnlyList<ValidationSuggestion> GenerateMechanicsSuggestions(GamePlan plan)
        {
            var suggestions = new List<ValidationSuggestion>();
            
            // Suggest adding genre-specific mechanics
            switch (plan.Genre)
            {
                case "FPS":
                    suggestions.Add(new ValidationSuggestion
                    {
                        Category = "Mechanics",
                        Title = "Add Reload Mechanics",
                        Description = "Consider adding reload mechanics to enhance the FPS experience.",
                        Priority = 3,
                        Effort = "Low"
                    });
                    break;
                case "Platformer":
                    suggestions.Add(new ValidationSuggestion
                    {
                        Category = "Mechanics",
                        Title = "Add Wall Jumping",
                        Description = "Consider adding wall jumping mechanics to increase platforming options.",
                        Priority = 4,
                        Effort = "Medium"
                    });
                    break;
                case "RPG":
                    suggestions.Add(new ValidationSuggestion
                    {
                        Category = "Mechanics",
                        Title = "Add Skill Trees",
                        Description = "Consider adding skill trees to provide character customization.",
                        Priority = 3,
                        Effort = "High"
                    });
                    break;
            }
            
            // Suggest adding accessibility mechanics
            suggestions.Add(new ValidationSuggestion
            {
                Category = "Mechanics",
                Title = "Add Accessibility Options",
                Description = "Consider adding accessibility options like colorblind support or difficulty toggles.",
                Priority = 4,
                Effort = "Medium"
            });
            
            return suggestions;
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

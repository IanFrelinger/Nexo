using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Genre profile for First-Person Shooter (FPS) games.
    /// Defines FPS-specific rules, budgets, and validation criteria.
    /// </summary>
    public sealed class FPSProfile : IGenreProfile
    {
        public string Id => "fps";
        public string Name => "First-Person Shooter";
        public string Description => "Fast-paced action games focused on shooting mechanics from a first-person perspective";
        
        public IReadOnlyList<string> Keywords => new[]
        {
            "shoot", "gun", "weapon", "aim", "fire", "combat", "battle", "war", "military", "soldier",
            "fps", "first-person", "shooter", "action", "fast-paced", "intense", "violent"
        };
        
        public IReadOnlyList<string> CoreMechanics => new[]
        {
            "Shoot", "Aim", "Reload", "Move", "Crouch", "Jump", "Sprint", "Grenade", "Melee"
        };
        
        public PerformanceBudgets PerformanceBudgets => new()
        {
            MaxTriangles = 150000, // FPS games can handle more geometry
            MaxDrawCalls = 150, // More draw calls for detailed environments
            MaxTextureMemoryMB = 1024, // High texture quality for realism
            MaxAudioMemoryMB = 256, // Rich audio for immersion
            MaxActiveLights = 12, // Dynamic lighting for atmosphere
            MaxParticles = 2000, // Explosions and effects
            MaxPhysicsObjects = 50, // Bullet physics and debris
            MaxAIAgents = 30 // Multiple enemies
        };
        
        public PacingConfiguration PacingConfiguration => new()
        {
            TargetBPM = 140.0f, // High intensity
            MinEventInterval = 3.0f, // Frequent action
            MaxEventInterval = 15.0f, // Never too quiet
            TargetInteractionDensity = 15.0f, // High interaction rate
            BreathingRoomRatio = 0.2f, // Minimal quiet time
            DifficultyRampRate = 0.15f, // Steep difficulty curve
            PacingCurveType = "exponential" // Intensifies over time
        };
        
        public AccessibilityDefaults AccessibilityDefaults => new()
        {
            ColorContrastRatio = 4.5f, // Standard contrast
            TextSizeMultiplier = 1.0f, // Standard text size
            CanReduceMotion = true, // Motion can be reduced
            CanSubstituteAudio = true, // Audio cues can be visual
            CanSubstituteColor = true, // Color can be substituted
            DifficultyOptions = new[] { "Easy", "Normal", "Hard", "Nightmare" },
            SupportsOneHandedPlay = false // Requires both hands
        };
        
        public IReadOnlyList<string> RequiredValidators => new[]
        {
            "PlayabilityValidator", "MechanicsValidator", "PerformanceValidator", "PacingValidator"
        };
        
        public IReadOnlyList<AssetRequirement> AssetRequirements => new[]
        {
            new AssetRequirement
            {
                AssetType = "Weapon",
                Name = "Primary Weapon",
                Description = "Main weapon for the player",
                IsRequired = true,
                Priority = 5
            },
            new AssetRequirement
            {
                AssetType = "Weapon",
                Name = "Secondary Weapon",
                Description = "Backup weapon for the player",
                IsRequired = false,
                Priority = 3
            },
            new AssetRequirement
            {
                AssetType = "Enemy",
                Name = "Enemy AI",
                Description = "AI enemies to fight",
                IsRequired = true,
                Priority = 4
            },
            new AssetRequirement
            {
                AssetType = "Environment",
                Name = "Cover Objects",
                Description = "Objects to take cover behind",
                IsRequired = true,
                Priority = 4
            },
            new AssetRequirement
            {
                AssetType = "Audio",
                Name = "Weapon Sounds",
                Description = "Audio for weapon firing and reloading",
                IsRequired = true,
                Priority = 3
            }
        };
        
        public DifficultyProgressionModel DifficultyProgression => new()
        {
            StartingDifficulty = 2, // Start easy
            PeakDifficulty = 5, // Can get very hard
            TimeToPeakMinutes = 3.0f, // Quick ramp-up
            DifficultyCurveType = "exponential", // Steep curve
            AllowDifficultyDecrease = false, // No difficulty reduction
            MinDifficultyChangeInterval = 10.0f // Quick changes allowed
        };
        
        public int DetectGenre(DesignBrief brief)
        {
            if (brief == null)
                return 0;
            
            var description = brief.Description.ToLowerInvariant();
            var hint = brief.GenreHint?.ToLowerInvariant() ?? string.Empty;
            
            var score = 0;
            
            // Check for explicit genre hint
            if (hint.Contains("fps") || hint.Contains("shooter"))
                score += 40;
            
            // Check for shooting keywords
            var shootingKeywords = new[] { "shoot", "gun", "weapon", "aim", "fire", "combat", "battle" };
            foreach (var keyword in shootingKeywords)
            {
                if (description.Contains(keyword))
                    score += 10;
            }
            
            // Check for FPS-specific terms
            var fpsKeywords = new[] { "first-person", "perspective", "crosshair", "reload", "ammo", "bullet" };
            foreach (var keyword in fpsKeywords)
            {
                if (description.Contains(keyword))
                    score += 8;
            }
            
            // Check for action/intensity keywords
            var actionKeywords = new[] { "fast", "intense", "action", "violent", "explosive", "dynamic" };
            foreach (var keyword in actionKeywords)
            {
                if (description.Contains(keyword))
                    score += 5;
            }
            
            // Check for military/combat context
            var militaryKeywords = new[] { "military", "soldier", "war", "battlefield", "tactical", "mission" };
            foreach (var keyword in militaryKeywords)
            {
                if (description.Contains(keyword))
                    score += 6;
            }
            
            // Check for difficulty level (FPS games often have high difficulty)
            if (brief.DifficultyLevel >= 4)
                score += 5;
            
            // Check for short duration (FPS games are often quick)
            if (brief.TargetDurationMinutes <= 5)
                score += 5;
            
            return Math.Min(100, score);
        }
        
        public IReadOnlyList<ValidationSuggestion> GetSuggestions(GamePlan plan)
        {
            var suggestions = new List<ValidationSuggestion>();
            
            // Check for missing FPS mechanics
            var requiredMechanics = new[] { "Shoot", "Aim", "Move" };
            var missingMechanics = requiredMechanics.Where(m => !plan.CoreMechanics.Any(cm => cm.Contains(m))).ToList();
            
            if (missingMechanics.Count > 0)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Mechanics",
                    Title = "Add Missing FPS Mechanics",
                    Description = $"Consider adding missing FPS mechanics: {string.Join(", ", missingMechanics)}",
                    Priority = 4,
                    Effort = "Low"
                });
            }
            
            // Check for weapon assets
            var hasWeaponAssets = plan.RequiredAssets.Any(a => a.AssetType == "Weapon");
            if (!hasWeaponAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Weapon Assets",
                    Description = "FPS games require weapon assets for shooting mechanics",
                    Priority = 5,
                    Effort = "Medium"
                });
            }
            
            // Check for enemy assets
            var hasEnemyAssets = plan.RequiredAssets.Any(a => a.AssetType == "Enemy" || a.Name.Contains("Enemy"));
            if (!hasEnemyAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Enemy Assets",
                    Description = "FPS games typically require enemy AI for combat",
                    Priority = 4,
                    Effort = "High"
                });
            }
            
            // Check for cover objects
            var hasCoverAssets = plan.RequiredAssets.Any(a => a.Name.Contains("Cover") || a.Name.Contains("Obstacle"));
            if (!hasCoverAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Cover Objects",
                    Description = "FPS games benefit from cover objects for tactical gameplay",
                    Priority = 3,
                    Effort = "Medium"
                });
            }
            
            // Check for audio assets
            var hasAudioAssets = plan.RequiredAssets.Any(a => a.AssetType == "Audio");
            if (!hasAudioAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Audio Assets",
                    Description = "FPS games require audio for weapon sounds and immersion",
                    Priority = 3,
                    Effort = "Medium"
                });
            }
            
            // Check for difficulty progression
            if (plan.DifficultyProgression.Count == 0)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Pacing",
                    Title = "Add Difficulty Progression",
                    Description = "FPS games benefit from clear difficulty progression",
                    Priority = 3,
                    Effort = "Low"
                });
            }
            
            return suggestions;
        }
        
        public ValidationResult ValidateGenreRequirements(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            var score = 100;
            
            // Check for required mechanics
            var requiredMechanics = new[] { "Shoot", "Aim", "Move" };
            var missingMechanics = requiredMechanics.Where(m => !plan.CoreMechanics.Any(cm => cm.Contains(m))).ToList();
            
            foreach (var mechanic in missingMechanics)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Mechanics",
                    Title = $"Missing Required FPS Mechanic: {mechanic}",
                    Description = $"FPS games require {mechanic} mechanics for core gameplay",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = $"Add {mechanic} to the core mechanics list"
                });
                score -= 20;
            }
            
            // Check for weapon assets
            var hasWeaponAssets = plan.RequiredAssets.Any(a => a.AssetType == "Weapon");
            if (!hasWeaponAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Category = "Assets",
                    Title = "Missing Weapon Assets",
                    Description = "FPS games require weapon assets for shooting mechanics",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Add weapon assets to the required assets list"
                });
                score -= 15;
            }
            
            // Check for enemy assets
            var hasEnemyAssets = plan.RequiredAssets.Any(a => a.AssetType == "Enemy" || a.Name.Contains("Enemy"));
            if (!hasEnemyAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Assets",
                    Title = "Missing Enemy Assets",
                    Description = "FPS games typically require enemy AI for combat",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Consider adding enemy assets for combat gameplay"
                });
                score -= 10;
            }
            
            // Check for appropriate difficulty level
            if (plan.SourceBrief.DifficultyLevel < 2)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Difficulty",
                    Title = "Low Difficulty for FPS",
                    Description = "FPS games typically have higher difficulty levels",
                    Location = "GamePlan.SourceBrief.DifficultyLevel",
                    SuggestedFix = "Consider increasing the difficulty level for FPS gameplay"
                });
                score -= 5;
            }
            
            // Check for appropriate duration
            if (plan.EstimatedDurationMinutes > 10)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Duration",
                    Title = "Long Duration for FPS",
                    Description = "FPS games are typically shorter, more intense experiences",
                    Location = "GamePlan.EstimatedDurationMinutes",
                    SuggestedFix = "Consider reducing the duration for more intense FPS gameplay"
                });
                score -= 5;
            }
            
            var message = issues.Count == 0 
                ? "FPS genre requirements validated successfully"
                : $"FPS genre validation found {issues.Count} issues";
            
            return new ValidationResult
            {
                IsValid = issues.Count == 0 || issues.All(i => i.Severity < ValidationSeverity.Critical),
                Score = Math.Max(0, score),
                Message = message,
                Details = GenerateFPSDetails(plan, issues),
                Issues = issues,
                Suggestions = GetSuggestions(plan)
            };
        }
        
        private static string GenerateFPSDetails(GamePlan plan, IReadOnlyList<ValidationIssue> issues)
        {
            var details = new List<string>
            {
                $"FPS Genre Validation",
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

using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Genre profile for Platformer games.
    /// Defines platformer-specific rules, budgets, and validation criteria.
    /// </summary>
    public sealed class PlatformerProfile : IGenreProfile
    {
        public string Id => "platformer";
        public string Name => "Platformer";
        public string Description => "Games focused on jumping and navigating platforms with precise movement";
        
        public IReadOnlyList<string> Keywords => new[]
        {
            "jump", "platform", "run", "move", "precise", "timing", "skill", "challenge",
            "platformer", "2d", "side-scrolling", "mario", "sonic", "metroid", "castlevania"
        };
        
        public IReadOnlyList<string> CoreMechanics => new[]
        {
            "Jump", "Run", "Wall Jump", "Double Jump", "Dash", "Crouch", "Slide", "Grab", "Swing"
        };
        
        public PerformanceBudgets PerformanceBudgets => new()
        {
            MaxTriangles = 80000, // Moderate geometry for 2D/3D platforms
            MaxDrawCalls = 80, // Moderate draw calls
            MaxTextureMemoryMB = 256, // Moderate texture usage
            MaxAudioMemoryMB = 128, // Moderate audio
            MaxActiveLights = 6, // Simple lighting
            MaxParticles = 500, // Minimal particles
            MaxPhysicsObjects = 200, // Many platforms and objects
            MaxAIAgents = 10 // Fewer enemies, more focus on platforming
        };
        
        public PacingConfiguration PacingConfiguration => new()
        {
            TargetBPM = 120.0f, // Moderate intensity
            MinEventInterval = 8.0f, // More breathing room
            MaxEventInterval = 25.0f, // Longer quiet periods
            TargetInteractionDensity = 8.0f, // Moderate interaction rate
            BreathingRoomRatio = 0.4f, // More quiet time
            DifficultyRampRate = 0.1f, // Gradual difficulty curve
            PacingCurveType = "linear" // Steady progression
        };
        
        public AccessibilityDefaults AccessibilityDefaults => new()
        {
            ColorContrastRatio = 4.5f, // Standard contrast
            TextSizeMultiplier = 1.2f, // Slightly larger text for readability
            CanReduceMotion = true, // Motion can be reduced
            CanSubstituteAudio = true, // Audio cues can be visual
            CanSubstituteColor = true, // Color can be substituted
            DifficultyOptions = new[] { "Easy", "Normal", "Hard", "Expert" },
            SupportsOneHandedPlay = true // Can be played with one hand
        };
        
        public IReadOnlyList<string> RequiredValidators => new[]
        {
            "PlayabilityValidator", "MechanicsValidator", "PerformanceValidator", "PacingValidator"
        };
        
        public IReadOnlyList<AssetRequirement> AssetRequirements => new[]
        {
            new AssetRequirement
            {
                AssetType = "Platform",
                Name = "Basic Platform",
                Description = "Standard platform for jumping",
                IsRequired = true,
                Priority = 5
            },
            new AssetRequirement
            {
                AssetType = "Platform",
                Name = "Moving Platform",
                Description = "Platform that moves or rotates",
                IsRequired = false,
                Priority = 3
            },
            new AssetRequirement
            {
                AssetType = "Collectible",
                Name = "Coin",
                Description = "Collectible items for the player",
                IsRequired = true,
                Priority = 4
            },
            new AssetRequirement
            {
                AssetType = "Hazard",
                Name = "Spike Trap",
                Description = "Hazardous obstacles",
                IsRequired = false,
                Priority = 2
            },
            new AssetRequirement
            {
                AssetType = "Powerup",
                Name = "Jump Boost",
                Description = "Power-up to enhance jumping",
                IsRequired = false,
                Priority = 3
            }
        };
        
        public DifficultyProgressionModel DifficultyProgression => new()
        {
            StartingDifficulty = 1, // Start very easy
            PeakDifficulty = 4, // Can get challenging
            TimeToPeakMinutes = 8.0f, // Gradual ramp-up
            DifficultyCurveType = "linear", // Steady progression
            AllowDifficultyDecrease = true, // Can have easier sections
            MinDifficultyChangeInterval = 60.0f // Slower changes
        };
        
        public int DetectGenre(DesignBrief brief)
        {
            if (brief == null)
                return 0;
            
            var description = brief.Description.ToLowerInvariant();
            var hint = brief.GenreHint?.ToLowerInvariant() ?? string.Empty;
            
            var score = 0;
            
            // Check for explicit genre hint
            if (hint.Contains("platformer") || hint.Contains("platform"))
                score += 40;
            
            // Check for jumping keywords
            var jumpingKeywords = new[] { "jump", "leap", "bounce", "hop", "spring" };
            foreach (var keyword in jumpingKeywords)
            {
                if (description.Contains(keyword))
                    score += 15;
            }
            
            // Check for platform keywords
            var platformKeywords = new[] { "platform", "ledge", "step", "block", "tile" };
            foreach (var keyword in platformKeywords)
            {
                if (description.Contains(keyword))
                    score += 12;
            }
            
            // Check for movement keywords
            var movementKeywords = new[] { "run", "move", "walk", "dash", "sprint" };
            foreach (var keyword in movementKeywords)
            {
                if (description.Contains(keyword))
                    score += 8;
            }
            
            // Check for precision/skill keywords
            var skillKeywords = new[] { "precise", "timing", "skill", "challenge", "difficult" };
            foreach (var keyword in skillKeywords)
            {
                if (description.Contains(keyword))
                    score += 6;
            }
            
            // Check for collectible keywords
            var collectibleKeywords = new[] { "collect", "coin", "item", "powerup", "upgrade" };
            foreach (var keyword in collectibleKeywords)
            {
                if (description.Contains(keyword))
                    score += 5;
            }
            
            // Check for 2D keywords
            var dimensionKeywords = new[] { "2d", "side-scrolling", "horizontal", "vertical" };
            foreach (var keyword in dimensionKeywords)
            {
                if (description.Contains(keyword))
                    score += 8;
            }
            
            // Check for difficulty level (platformers can have various difficulty)
            if (brief.DifficultyLevel >= 2 && brief.DifficultyLevel <= 4)
                score += 5;
            
            // Check for moderate duration
            if (brief.TargetDurationMinutes >= 3 && brief.TargetDurationMinutes <= 8)
                score += 5;
            
            return Math.Min(100, score);
        }
        
        public IReadOnlyList<ValidationSuggestion> GetSuggestions(GamePlan plan)
        {
            var suggestions = new List<ValidationSuggestion>();
            
            // Check for missing platformer mechanics
            var requiredMechanics = new[] { "Jump", "Run", "Move" };
            var missingMechanics = requiredMechanics.Where(m => !plan.CoreMechanics.Any(cm => cm.Contains(m))).ToList();
            
            if (missingMechanics.Count > 0)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Mechanics",
                    Title = "Add Missing Platformer Mechanics",
                    Description = $"Consider adding missing platformer mechanics: {string.Join(", ", missingMechanics)}",
                    Priority = 4,
                    Effort = "Low"
                });
            }
            
            // Check for platform assets
            var hasPlatformAssets = plan.RequiredAssets.Any(a => a.AssetType == "Platform" || a.Name.Contains("Platform"));
            if (!hasPlatformAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Platform Assets",
                    Description = "Platformer games require platform assets for jumping mechanics",
                    Priority = 5,
                    Effort = "Medium"
                });
            }
            
            // Check for collectible assets
            var hasCollectibleAssets = plan.RequiredAssets.Any(a => a.AssetType == "Collectible" || a.Name.Contains("Coin"));
            if (!hasCollectibleAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Collectible Assets",
                    Description = "Platformer games often include collectible items for engagement",
                    Priority = 3,
                    Effort = "Low"
                });
            }
            
            // Check for hazard assets
            var hasHazardAssets = plan.RequiredAssets.Any(a => a.AssetType == "Hazard" || a.Name.Contains("Spike"));
            if (!hasHazardAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Hazard Assets",
                    Description = "Platformer games benefit from hazards to increase challenge",
                    Priority = 2,
                    Effort = "Medium"
                });
            }
            
            // Check for power-up assets
            var hasPowerupAssets = plan.RequiredAssets.Any(a => a.AssetType == "Powerup" || a.Name.Contains("Boost"));
            if (!hasPowerupAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Power-up Assets",
                    Description = "Platformer games can include power-ups for enhanced gameplay",
                    Priority = 2,
                    Effort = "Medium"
                });
            }
            
            // Check for appropriate difficulty progression
            if (plan.DifficultyProgression.Count == 0)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Pacing",
                    Title = "Add Difficulty Progression",
                    Description = "Platformer games benefit from gradual difficulty progression",
                    Priority = 3,
                    Effort = "Low"
                });
            }
            
            // Check for breathing room
            var hasBreathingRoom = plan.PlayerExperience.Any(pe => pe.Contains("Relaxing") || pe.Contains("Calm"));
            if (!hasBreathingRoom)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Pacing",
                    Title = "Add Breathing Room",
                    Description = "Platformer games benefit from moments of calm between challenges",
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
            var requiredMechanics = new[] { "Jump", "Run", "Move" };
            var missingMechanics = requiredMechanics.Where(m => !plan.CoreMechanics.Any(cm => cm.Contains(m))).ToList();
            
            foreach (var mechanic in missingMechanics)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Mechanics",
                    Title = $"Missing Required Platformer Mechanic: {mechanic}",
                    Description = $"Platformer games require {mechanic} mechanics for core gameplay",
                    Location = "GamePlan.CoreMechanics",
                    SuggestedFix = $"Add {mechanic} to the core mechanics list"
                });
                score -= 20;
            }
            
            // Check for platform assets
            var hasPlatformAssets = plan.RequiredAssets.Any(a => a.AssetType == "Platform" || a.Name.Contains("Platform"));
            if (!hasPlatformAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Assets",
                    Title = "Missing Platform Assets",
                    Description = "Platformer games require platform assets for jumping mechanics",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Add platform assets to the required assets list"
                });
                score -= 20;
            }
            
            // Check for collectible assets
            var hasCollectibleAssets = plan.RequiredAssets.Any(a => a.AssetType == "Collectible" || a.Name.Contains("Coin"));
            if (!hasCollectibleAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Assets",
                    Title = "Missing Collectible Assets",
                    Description = "Platformer games typically include collectible items for engagement",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Consider adding collectible assets for player engagement"
                });
                score -= 10;
            }
            
            // Check for appropriate difficulty level
            if (plan.SourceBrief.DifficultyLevel > 4)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Difficulty",
                    Title = "High Difficulty for Platformer",
                    Description = "Platformer games typically have moderate difficulty levels",
                    Location = "GamePlan.SourceBrief.DifficultyLevel",
                    SuggestedFix = "Consider reducing the difficulty level for platformer gameplay"
                });
                score -= 5;
            }
            
            // Check for appropriate duration
            if (plan.EstimatedDurationMinutes > 15)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Duration",
                    Title = "Long Duration for Platformer",
                    Description = "Platformer games are typically shorter, focused experiences",
                    Location = "GamePlan.EstimatedDurationMinutes",
                    SuggestedFix = "Consider reducing the duration for more focused platformer gameplay"
                });
                score -= 5;
            }
            
            // Check for breathing room in player experience
            var hasBreathingRoom = plan.PlayerExperience.Any(pe => pe.Contains("Relaxing") || pe.Contains("Calm"));
            if (!hasBreathingRoom)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Pacing",
                    Title = "Missing Breathing Room",
                    Description = "Platformer games benefit from moments of calm between challenges",
                    Location = "GamePlan.PlayerExperience",
                    SuggestedFix = "Consider adding relaxing or calm elements to the player experience"
                });
                score -= 5;
            }
            
            var message = issues.Count == 0 
                ? "Platformer genre requirements validated successfully"
                : $"Platformer genre validation found {issues.Count} issues";
            
            return new ValidationResult
            {
                IsValid = issues.Count == 0 || issues.All(i => i.Severity < ValidationSeverity.Critical),
                Score = Math.Max(0, score),
                Message = message,
                Details = GeneratePlatformerDetails(plan, issues),
                Issues = issues,
                Suggestions = GetSuggestions(plan)
            };
        }
        
        private static string GeneratePlatformerDetails(GamePlan plan, IReadOnlyList<ValidationIssue> issues)
        {
            var details = new List<string>
            {
                $"Platformer Genre Validation",
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

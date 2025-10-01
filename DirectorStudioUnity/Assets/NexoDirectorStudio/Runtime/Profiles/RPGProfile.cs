using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Genre profile for Role-Playing Game (RPG) games.
    /// Defines RPG-specific rules, budgets, and validation criteria.
    /// </summary>
    public sealed class RPGProfile : IGenreProfile
    {
        public string Id => "rpg";
        public string Name => "Role-Playing Game";
        public string Description => "Games focused on character development, story, and strategic gameplay";
        
        public IReadOnlyList<string> Keywords => new[]
        {
            "rpg", "role-playing", "character", "story", "quest", "level", "experience", "skill",
            "inventory", "dialogue", "narrative", "adventure", "fantasy", "medieval", "magic"
        };
        
        public IReadOnlyList<string> CoreMechanics => new[]
        {
            "Talk", "Quest", "Level Up", "Inventory", "Combat", "Explore", "Craft", "Trade", "Dialogue"
        };
        
        public PerformanceBudgets PerformanceBudgets => new()
        {
            MaxTriangles = 120000, // Moderate geometry for detailed environments
            MaxDrawCalls = 100, // Moderate draw calls
            MaxTextureMemoryMB = 768, // High texture quality for detailed environments
            MaxAudioMemoryMB = 512, // Rich audio for dialogue and music
            MaxActiveLights = 10, // Atmospheric lighting
            MaxParticles = 800, // Moderate particles for effects
            MaxPhysicsObjects = 150, // Many interactive objects
            MaxAIAgents = 25 // Many NPCs and enemies
        };
        
        public PacingConfiguration PacingConfiguration => new()
        {
            TargetBPM = 100.0f, // Lower intensity, more thoughtful
            MinEventInterval = 15.0f, // Longer quiet periods
            MaxEventInterval = 45.0f, // Much longer quiet periods
            TargetInteractionDensity = 5.0f, // Lower interaction rate
            BreathingRoomRatio = 0.6f, // More quiet time for exploration
            DifficultyRampRate = 0.05f, // Very gradual difficulty curve
            PacingCurveType = "logarithmic" // Slow, steady progression
        };
        
        public AccessibilityDefaults AccessibilityDefaults => new()
        {
            ColorContrastRatio = 4.5f, // Standard contrast
            TextSizeMultiplier = 1.3f, // Larger text for dialogue
            CanReduceMotion = true, // Motion can be reduced
            CanSubstituteAudio = true, // Audio cues can be visual
            CanSubstituteColor = true, // Color can be substituted
            DifficultyOptions = new[] { "Story", "Normal", "Hard", "Expert" },
            SupportsOneHandedPlay = true // Can be played with one hand
        };
        
        public IReadOnlyList<string> RequiredValidators => new[]
        {
            "PlayabilityValidator", "MechanicsValidator", "PerformanceValidator", "PacingValidator"
        };
        
        public IReadOnlyList<AssetRequirement> AssetRequirements => new[]
        {
            new AssetRequirement("NPC", "Quest Giver", "NPC that provides quests to the player", true, 5),
            new AssetRequirement("Quest", "Main Quest", "Primary quest for the player", true, 5),
            new AssetRequirement("Inventory", "Player Inventory", "Inventory system for the player", true, 4),
            new AssetRequirement("Character", "Player Character", "Player character with stats and abilities", true, 5),
            new AssetRequirement("Dialogue", "Dialogue System", "System for character dialogue", true, 4)
        };
        
        public DifficultyProgressionModel DifficultyProgression => new()
        {
            StartingDifficulty = 1, // Start very easy
            PeakDifficulty = 3, // Moderate peak difficulty
            TimeToPeakMinutes = 15.0f, // Very gradual ramp-up
            DifficultyCurveType = "logarithmic", // Slow progression
            AllowDifficultyDecrease = true, // Can have easier sections
            MinDifficultyChangeInterval = 120.0f // Very slow changes
        };
        
        public int DetectGenre(DesignBrief brief)
        {
            if (brief == null)
                return 0;
            
            var description = brief.Description.ToLowerInvariant();
            var hint = brief.GenreHint?.ToLowerInvariant() ?? string.Empty;
            
            var score = 0;
            
            // Check for explicit genre hint
            if (hint.Contains("rpg") || hint.Contains("role-playing"))
                score += 40;
            
            // Check for RPG keywords
            var rpgKeywords = new[] { "quest", "character", "level", "experience", "skill", "inventory" };
            foreach (var keyword in rpgKeywords)
            {
                if (description.Contains(keyword))
                    score += 12;
            }
            
            // Check for story keywords
            var storyKeywords = new[] { "story", "narrative", "dialogue", "conversation", "talk" };
            foreach (var keyword in storyKeywords)
            {
                if (description.Contains(keyword))
                    score += 10;
            }
            
            // Check for adventure keywords
            var adventureKeywords = new[] { "adventure", "explore", "discover", "journey", "travel" };
            foreach (var keyword in adventureKeywords)
            {
                if (description.Contains(keyword))
                    score += 8;
            }
            
            // Check for fantasy keywords
            var fantasyKeywords = new[] { "fantasy", "magic", "medieval", "dragon", "sword", "spell" };
            foreach (var keyword in fantasyKeywords)
            {
                if (description.Contains(keyword))
                    score += 6;
            }
            
            // Check for progression keywords
            var progressionKeywords = new[] { "progress", "develop", "grow", "improve", "advance" };
            foreach (var keyword in progressionKeywords)
            {
                if (description.Contains(keyword))
                    score += 5;
            }
            
            // Check for difficulty level (RPGs can have various difficulty)
            if (brief.DifficultyLevel >= 1 && brief.DifficultyLevel <= 3)
                score += 5;
            
            // Check for longer duration
            if (brief.TargetDurationMinutes >= 5 && brief.TargetDurationMinutes <= 20)
                score += 5;
            
            return Math.Min(100, score);
        }
        
        public IReadOnlyList<ValidationSuggestion> GetSuggestions(GamePlan plan)
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
            
            // Check for NPC assets
            var hasNPCAssets = plan.RequiredAssets.Any(a => a.AssetType == "NPC" || a.Name.Contains("NPC"));
            if (!hasNPCAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add NPC Assets",
                    Description = "RPG games require NPCs for dialogue and quests",
                    Priority = 5,
                    Effort = "High"
                });
            }
            
            // Check for quest assets
            var hasQuestAssets = plan.RequiredAssets.Any(a => a.AssetType == "Quest" || a.Name.Contains("Quest"));
            if (!hasQuestAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Quest Assets",
                    Description = "RPG games require quests for structure and progression",
                    Priority = 5,
                    Effort = "High"
                });
            }
            
            // Check for inventory assets
            var hasInventoryAssets = plan.RequiredAssets.Any(a => a.AssetType == "Inventory" || a.Name.Contains("Inventory"));
            if (!hasInventoryAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Inventory Assets",
                    Description = "RPG games require inventory systems for item management",
                    Priority = 4,
                    Effort = "Medium"
                });
            }
            
            // Check for dialogue assets
            var hasDialogueAssets = plan.RequiredAssets.Any(a => a.AssetType == "Dialogue" || a.Name.Contains("Dialogue"));
            if (!hasDialogueAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Dialogue Assets",
                    Description = "RPG games require dialogue systems for character interaction",
                    Priority = 4,
                    Effort = "Medium"
                });
            }
            
            // Check for character assets
            var hasCharacterAssets = plan.RequiredAssets.Any(a => a.AssetType == "Character" || a.Name.Contains("Character"));
            if (!hasCharacterAssets)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Assets",
                    Title = "Add Character Assets",
                    Description = "RPG games require character systems for progression",
                    Priority = 4,
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
        
        public ValidationResult ValidateGenreRequirements(GamePlan plan)
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
            
            // Check for NPC assets
            var hasNPCAssets = plan.RequiredAssets.Any(a => a.AssetType == "NPC" || a.Name.Contains("NPC"));
            if (!hasNPCAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Assets",
                    Title = "Missing NPC Assets",
                    Description = "RPG games require NPCs for dialogue and quests",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Add NPC assets to the required assets list"
                });
                score -= 20;
            }
            
            // Check for quest assets
            var hasQuestAssets = plan.RequiredAssets.Any(a => a.AssetType == "Quest" || a.Name.Contains("Quest"));
            if (!hasQuestAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Assets",
                    Title = "Missing Quest Assets",
                    Description = "RPG games require quests for structure and progression",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Add quest assets to the required assets list"
                });
                score -= 20;
            }
            
            // Check for inventory assets
            var hasInventoryAssets = plan.RequiredAssets.Any(a => a.AssetType == "Inventory" || a.Name.Contains("Inventory"));
            if (!hasInventoryAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Assets",
                    Title = "Missing Inventory Assets",
                    Description = "RPG games typically include inventory systems for item management",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Consider adding inventory assets for item management"
                });
                score -= 10;
            }
            
            // Check for dialogue assets
            var hasDialogueAssets = plan.RequiredAssets.Any(a => a.AssetType == "Dialogue" || a.Name.Contains("Dialogue"));
            if (!hasDialogueAssets)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Category = "Assets",
                    Title = "Missing Dialogue Assets",
                    Description = "RPG games typically include dialogue systems for character interaction",
                    Location = "GamePlan.RequiredAssets",
                    SuggestedFix = "Consider adding dialogue assets for character interaction"
                });
                score -= 10;
            }
            
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
                Errors = issues.Where(i => i.Severity >= ValidationSeverity.Error).Select(i => i.Description).ToArray(),
                Warnings = issues.Where(i => i.Severity == ValidationSeverity.Warning).Select(i => i.Description).ToArray()
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

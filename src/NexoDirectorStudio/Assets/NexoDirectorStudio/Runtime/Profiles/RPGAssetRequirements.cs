using NexoDirectorStudio.DTO;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Asset requirements and validation for RPG genre
    /// </summary>
    public static class RPGAssetRequirements
    {
        public static IReadOnlyList<AssetRequirement> AssetRequirements => new[]
        {
            new AssetRequirement
            {
                AssetType = "NPC",
                Name = "Quest Giver",
                Description = "NPC that provides quests to the player",
                IsRequired = true,
                Priority = 5
            },
            new AssetRequirement
            {
                AssetType = "Quest",
                Name = "Main Quest",
                Description = "Primary quest for the player",
                IsRequired = true,
                Priority = 5
            },
            new AssetRequirement
            {
                AssetType = "Inventory",
                Name = "Player Inventory",
                Description = "Inventory system for the player",
                IsRequired = true,
                Priority = 4
            },
            new AssetRequirement
            {
                AssetType = "Character",
                Name = "Player Character",
                Description = "Player character with stats and abilities",
                IsRequired = true,
                Priority = 5
            },
            new AssetRequirement
            {
                AssetType = "Dialogue",
                Name = "Dialogue System",
                Description = "System for character dialogue",
                IsRequired = true,
                Priority = 4
            }
        };
        
        public static IReadOnlyList<ValidationSuggestion> GetAssetSuggestions(GamePlan plan)
        {
            var suggestions = new List<ValidationSuggestion>();
            
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
            
            return suggestions;
        }
        
        public static IReadOnlyList<ValidationIssue> ValidateAssetRequirements(GamePlan plan)
        {
            var issues = new List<ValidationIssue>();
            
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
            }
            
            return issues;
        }
    }
}
